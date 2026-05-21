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

    public MatchmakingService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task JoinQueueAsync(Guid userId, GameMode mode, Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType)
    {
        // Önce varsa eski kaydını temizle
        await LeaveQueueAsync(userId);
        
        string userIdStr = userId.ToString();
        int rounds = (int)mode;

        // Meta veriyi kaydet (Kategori vb.)
        var metaKey = RedisKeys.MatchmakingUserMetadata(userIdStr);
        await _db.HashSetAsync(metaKey, new HashEntry[] 
        { 
            new("mode", rounds),
            new("categoryId", categoryId?.ToString() ?? ""),
            new("difficulty", difficulty.HasValue ? ((int)difficulty.Value).ToString() : ""),
            new("questionType", questionType.HasValue ? ((int)questionType.Value).ToString() : ""),
            new("joinedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        });
        await _db.KeyExpireAsync(metaKey, TimeSpan.FromMinutes(30));

        // Moda özel kuyruğa ekle
        await _db.ListRightPushAsync(RedisKeys.MatchmakingQueue(rounds), userIdStr);
    }

    public async Task LeaveQueueAsync(Guid userId)
    {
        string userIdStr = userId.ToString();
        
        // Önce meta veriyi bulup hangi kuyrukta olduğunu anlamaya çalışabiliriz 
        // veya her iki kuyruktan da silebiliriz (daha güvenli/basit)
        await _db.ListRemoveAsync(RedisKeys.MatchmakingQueue((int)GameMode.Short), userIdStr);
        await _db.ListRemoveAsync(RedisKeys.MatchmakingQueue((int)GameMode.Long), userIdStr);
        
        await _db.KeyDeleteAsync(RedisKeys.MatchmakingUserMetadata(userIdStr));
    }

    public async Task<(Guid Player1, Guid Player2)?> TryMatchAsync(GameMode mode)
    {
        int rounds = (int)mode;
        string queueKey = RedisKeys.MatchmakingQueue(rounds);

        long queueLength = await _db.ListLengthAsync(queueKey);
        
        if (queueLength < 2)
        {
            return null;
        }

        var p1Str = await _db.ListLeftPopAsync(queueKey);
        var p2Str = await _db.ListLeftPopAsync(queueKey);

        if (p1Str.IsNull || p2Str.IsNull)
        {
            return null;
        }

        return (Guid.Parse(p1Str!), Guid.Parse(p2Str!));
    }
}
