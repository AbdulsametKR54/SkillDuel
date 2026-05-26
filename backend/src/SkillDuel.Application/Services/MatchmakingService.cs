using StackExchange.Redis;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Constants;
using SkillDuel.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace SkillDuel.Application.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly IUserRepository _userRepository;

    public MatchmakingService(IConnectionMultiplexer redis, IUserRepository userRepository)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _userRepository = userRepository;
    }

    public async Task JoinQueueAsync(Guid userId, GameMode mode, Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType)
    {
        // First clean up any old record
        await LeaveQueueAsync(userId);
        
        int rounds = (int)mode;
        var user = await _userRepository.GetByIdAsync(userId);
        int elo = user?.EloRating ?? 1200;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Serialize metadata inside a unified token to avoid any HGETALL lookups completely
        string memberStr = MatchmakingSerializer.Serialize(
            userId,
            categoryId ?? Guid.Empty,
            difficulty.HasValue ? (int)difficulty.Value : -1,
            questionType.HasValue ? (int)questionType.Value : -1,
            elo,
            now
        );

        // Store player directly in ELO Score Sorted Set (ZSET)
        await _db.SortedSetAddAsync(RedisKeys.MatchmakingQueue(rounds), memberStr, elo);

        // Trigger matchmaking engine background BLPOP queue (wake worker instantly)
        await _db.ListLeftPushAsync(RedisKeysExtensions.MatchmakingTriggerQueue, "trigger");
    }

    public async Task LeaveQueueAsync(Guid userId)
    {
        string userIdStr = userId.ToString();

        // Remove from both ELO Sorted Set (ZSET) queues
        var shortQueue = RedisKeys.MatchmakingQueue((int)GameMode.Short);
        var longQueue = RedisKeys.MatchmakingQueue((int)GameMode.Long);

        // Scan members to match by prefix since userId is the prefix of our serialized queue payload
        await RemovePlayerFromZSetAsync(shortQueue, userIdStr);
        await RemovePlayerFromZSetAsync(longQueue, userIdStr);
    }

    public async Task<(Guid Player1, Guid Player2)?> TryMatchAsync(GameMode mode)
    {
        // Keeping this method signature to satisfy interface contract while refactoring
        return null;
    }

    private async Task RemovePlayerFromZSetAsync(string queueKey, string userIdStr)
    {
        var members = await _db.SortedSetRangeByRankAsync(queueKey, 0, -1);
        foreach (var member in members)
        {
            string val = member.ToString();
            if (val.StartsWith(userIdStr + ":"))
            {
                await _db.SortedSetRemoveAsync(queueKey, val);
            }
        }
    }
}
