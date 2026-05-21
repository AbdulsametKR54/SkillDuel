using Microsoft.AspNetCore.SignalR;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Domain.Constants;
using SkillDuel.Api.Hubs;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace SkillDuel.Api.Jobs;

public class MatchmakingJob
{
    private readonly IMatchmakingService _matchmakingService;
    private readonly IGameService _gameService;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IUserRepository _userRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameNotificationService _notificationService;

    public MatchmakingJob(
        IMatchmakingService matchmakingService,
        IGameService gameService,
        IConnectionMultiplexer redis,
        IHubContext<GameHub> hubContext,
        IUserRepository userRepository,
        IGameSessionRepository gameSessionRepository,
        IUnitOfWork unitOfWork,
        IGameNotificationService notificationService)
    {
        _matchmakingService = matchmakingService;
        _gameService = gameService;
        _redis = redis;
        _hubContext = hubContext;
        _userRepository = userRepository;
        _gameSessionRepository = gameSessionRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task RunAsync()
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
        {
            int rounds = (int)mode;
            string queueKey = RedisKeys.MatchmakingQueue(rounds);
            
            var playersInQueue = await db.ListRangeAsync(queueKey);
            if (playersInQueue.Length == 0) continue;

            var handledPlayers = new HashSet<string>();

            for (int i = 0; i < playersInQueue.Length; i++)
            {
                string p1IdStr = playersInQueue[i]!;
                if (handledPlayers.Contains(p1IdStr)) continue;

                var p1Meta = await GetUserMetadataAsync(db, p1IdStr);
                if (p1Meta == null) 
                {
                    await db.ListRemoveAsync(queueKey, p1IdStr);
                    continue;
                }

                // Check Timeout (60 seconds)
                if (now - p1Meta.JoinedAt > 60)
                {
                    handledPlayers.Add(p1IdStr);
                    await db.ListRemoveAsync(queueKey, p1IdStr);
                    await db.KeyDeleteAsync(RedisKeys.MatchmakingUserMetadata(p1IdStr));
                    await _notificationService.SendMatchmakingTimeoutAsync(Guid.Parse(p1IdStr));
                    continue;
                }

                // Search for match
                for (int j = i + 1; j < playersInQueue.Length; j++)
                {
                    string p2IdStr = playersInQueue[j]!;
                    if (handledPlayers.Contains(p2IdStr)) continue;

                    var p2Meta = await GetUserMetadataAsync(db, p2IdStr);
                    if (p2Meta == null)
                    {
                        await db.ListRemoveAsync(queueKey, p2IdStr);
                        continue;
                    }

                    // Strict matching: difficulty + questionType must be identical
                    if (p1Meta.Difficulty == p2Meta.Difficulty && p1Meta.QuestionType == p2Meta.QuestionType)
                    {
                        handledPlayers.Add(p1IdStr);
                        handledPlayers.Add(p2IdStr);

                        // Remove from queue
                        await db.ListRemoveAsync(queueKey, p1IdStr);
                        await db.ListRemoveAsync(queueKey, p2IdStr);

                        // Match Found
                        await CreateAndStartSessionAsync(Guid.Parse(p1IdStr), Guid.Parse(p2IdStr), mode, p1Meta, p2Meta);
                        break;
                    }
                }
            }
        }
    }

    private async Task<UserMatchMetadata?> GetUserMetadataAsync(IDatabase db, string userIdStr)
    {
        var meta = await db.HashGetAllAsync(RedisKeys.MatchmakingUserMetadata(userIdStr));
        if (meta.Length == 0) return null;

        var result = new UserMatchMetadata();
        
        var catStr = meta.FirstOrDefault(x => x.Name == "categoryId").Value;
        var diffStr = meta.FirstOrDefault(x => x.Name == "difficulty").Value;
        var typeStr = meta.FirstOrDefault(x => x.Name == "questionType").Value;
        var joinedAtStr = meta.FirstOrDefault(x => x.Name == "joinedAt").Value;

        if (!string.IsNullOrEmpty(catStr)) result.CategoryId = Guid.Parse(catStr!);
        if (!string.IsNullOrEmpty(diffStr)) result.Difficulty = (DifficultyLevel)int.Parse(diffStr!);
        if (!string.IsNullOrEmpty(typeStr)) result.QuestionType = (QuestionType)int.Parse(typeStr!);
        if (!string.IsNullOrEmpty(joinedAtStr)) result.JoinedAt = long.Parse(joinedAtStr!);

        return result;
    }

    private async Task CreateAndStartSessionAsync(Guid p1Id, Guid p2Id, GameMode mode, UserMatchMetadata p1Meta, UserMatchMetadata p2Meta)
    {
        var player1 = await _userRepository.GetByIdAsync(p1Id);
        var player2 = await _userRepository.GetByIdAsync(p2Id);

        if (player1 == null || player2 == null) return;

        var session = new GameSession
        {
            Player1Id = p1Id,
            Player2Id = p2Id,
            Status = GameStatus.Active
        };

        await _gameSessionRepository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Notify Players
        await _hubContext.Clients.User(p1Id.ToString()).SendAsync("MatchFound", new
        {
            SessionId = session.Id,
            MyId = p1Id,
            Players = new[] { 
                new { Id = p1Id, Username = player1.Username, Elo = player1.EloRating },
                new { Id = p2Id, Username = player2.Username, Elo = player2.EloRating }
            }
        });

        await _hubContext.Clients.User(p2Id.ToString()).SendAsync("MatchFound", new
        {
            SessionId = session.Id,
            MyId = p2Id,
            Players = new[] { 
                new { Id = p1Id, Username = player1.Username, Elo = player1.EloRating },
                new { Id = p2Id, Username = player2.Username, Elo = player2.EloRating }
            }
        });

        // Start Game
        await _gameService.StartGameAsync(session.Id, mode, 
            p1Meta.CategoryId, p2Meta.CategoryId, 
            p1Meta.Difficulty, p2Meta.Difficulty, 
            p1Meta.QuestionType, p2Meta.QuestionType,
            p1Id, p2Id,
            player1.Username, player2.Username);
    }

    private class UserMatchMetadata
    {
        public Guid? CategoryId { get; set; }
        public DifficultyLevel? Difficulty { get; set; }
        public QuestionType? QuestionType { get; set; }
        public long JoinedAt { get; set; }
    }
}
