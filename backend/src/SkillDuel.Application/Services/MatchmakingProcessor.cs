using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Domain.Constants;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Application.Services;

public class MatchmakingProcessor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly IUserRepository _userRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameService _gameService;
    private readonly ILogger<MatchmakingProcessor> _logger;

    // SignalR helper dependency inside application layer using generic hub context to stay decoupled
    private readonly IHubContext<GameHub, IGameHub> _hubContext;

    // Atomic Lua Script to securely claim two matched players synchronously across multiple nodes
    private const string MatchClaimLua = @"
        local queue = KEYS[1]
        local p1 = ARGV[1]
        local p2 = ARGV[2]
        
        local s1 = redis.call('ZSCORE', queue, p1)
        local s2 = redis.call('ZSCORE', queue, p2)
        
        if s1 and s2 then
            redis.call('ZREM', queue, p1)
            redis.call('ZREM', queue, p2)
            return 1
        else
            return 0
        end
    ";

    public MatchmakingProcessor(
        IConnectionMultiplexer redis,
        IUserRepository userRepository,
        IGameSessionRepository gameSessionRepository,
        IUnitOfWork unitOfWork,
        IGameService gameService,
        ILogger<MatchmakingProcessor> logger,
        IHubContext<GameHub, IGameHub> hubContext)  // direkt inject et
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _userRepository = userRepository;
        _gameSessionRepository = gameSessionRepository;
        _unitOfWork = unitOfWork;
        _gameService = gameService;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task EvaluateAllQueuesAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
        {
            int rounds = (int)mode;
            string queueKey = RedisKeys.MatchmakingQueue(rounds);

            // Bounded fast-exit check
            long queueLength = await _db.SortedSetLengthAsync(queueKey);
            if (queueLength < 2) continue;

            // Fetch players ordered by ELO
            var rawMembers = await _db.SortedSetRangeByRankAsync(queueKey, 0, -1);

            var players = rawMembers
                .Select(x => MatchmakingSerializer.Deserialize(x.ToString()))
                .Where(p => p != null)
                .Select(p => p!)
                .ToList();

            var handled = new HashSet<Guid>();

            for (int i = 0; i < players.Count; i++)
            {
                var p1 = players[i];
                if (handled.Contains(p1.UserId)) continue;

                // Handle Timed Out Players (60 seconds)
                if (now - p1.JoinedAt > 60)
                {
                    handled.Add(p1.UserId);
                    await _db.SortedSetRemoveAsync(queueKey, p1.RawValue);
                    _logger.LogInformation("Matchmaker: Timed out player {UserId}", p1.UserId);
                    continue;
                }

                // ELO-bounded range matchmaking scan
                for (int j = i + 1; j < players.Count; j++)
                {
                    var p2 = players[j];
                    if (handled.Contains(p2.UserId)) continue;

                    // Match criteria: Same Category, Same Difficulty, Same Question Type
                    if (p1.CategoryId == p2.CategoryId &&
                        p1.Difficulty == p2.Difficulty &&
                        p1.QuestionType == p2.QuestionType)
                    {
                        // Check if they are within acceptable Elo proximity range (expanding based on queue wait time)
                        int eloDifference = Math.Abs(p1.Elo - p2.Elo);
                        long minWait = Math.Max(now - p1.JoinedAt, now - p2.JoinedAt);
                        
                        // Start with 100 Elo limit, expand by 20 Elo every 5 seconds waiting
                        long allowedEloDiff = 100 + (minWait / 5) * 20;

                        if (eloDifference <= allowedEloDiff)
                        {
                            handled.Add(p1.UserId);
                            handled.Add(p2.UserId);

                            // Perform Atomic Double-Claim Match using Lua script
                            var atomicResult = await _db.ScriptEvaluateAsync(
                                MatchClaimLua,
                                new RedisKey[] { queueKey },
                                new RedisValue[] { p1.RawValue, p2.RawValue }
                            );

                            if ((int)atomicResult == 1)
                            {
                                _logger.LogInformation("Matchmaker: Concurrency-safe match created between {P1} and {P2}", p1.UserId, p2.UserId);
                                await CreateAndStartSessionAsync(p1, p2, mode);
                            }
                            else
                            {
                                _logger.LogWarning("Matchmaker: Concurrency race condition prevented double-matching on {P1} / {P2}", p1.UserId, p2.UserId);
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    private async Task CreateAndStartSessionAsync(MatchmakingMember p1, MatchmakingMember p2, GameMode mode)
    {
        var player1 = await _userRepository.GetByIdAsync(p1.UserId);
        var player2 = await _userRepository.GetByIdAsync(p2.UserId);

        if (player1 == null || player2 == null) return;

        var session = new GameSession
        {
            Player1Id = p1.UserId,
            Player2Id = p2.UserId,
            Status = GameStatus.Active
        };

        await _gameSessionRepository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        var players = new[]
        {
            new { Id = p1.UserId, Username = player1.Username, Elo = player1.EloRating },
            new { Id = p2.UserId, Username = player2.Username, Elo = player2.EloRating }
        };

        await _hubContext.Clients.User(p1.UserId.ToString()).MatchFound(new
        {
            SessionId = session.Id,
            MyId = p1.UserId,
            Players = players
        });

        await _hubContext.Clients.User(p2.UserId.ToString()).MatchFound(new
        {
            SessionId = session.Id,
            MyId = p2.UserId,
            Players = players
        });

        await _gameService.StartGameAsync(
            session.Id,
            mode,
            p1.CategoryId == Guid.Empty ? null : p1.CategoryId,
            p2.CategoryId == Guid.Empty ? null : p2.CategoryId,
            p1.Difficulty == -1 ? null : (DifficultyLevel)p1.Difficulty,
            p2.Difficulty == -1 ? null : (DifficultyLevel)p2.Difficulty,
            p1.QuestionType == -1 ? null : (QuestionType)p1.QuestionType,
            p2.QuestionType == -1 ? null : (QuestionType)p2.QuestionType,
            p1.UserId,
            p2.UserId,
            player1.Username,
            player2.Username
        );
    }
}
