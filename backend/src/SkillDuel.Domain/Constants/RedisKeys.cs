namespace SkillDuel.Domain.Constants;

public static class RedisKeys
{
    private const string Prefix = "skillduel:";

    // Matchmaking
    public static string MatchmakingQueue(int rounds) => $"{Prefix}matchmaking:queue:{rounds}";
    public const string MatchmakingTriggerQueue = "skillduel:matchmaking:trigger";
    public const string MatchmakingProcessingQueue = "skillduel:matchmaking:processing";

    public static string MatchmakingUserMetadata(string userId) => $"{Prefix}matchmaking:user:{userId}";

    // Game
    public static string GameState(string sessionId) => $"{Prefix}game:{sessionId}:state";
    
    public const string OnlineUsers = $"{Prefix}online:users";
    
    public const string LeaderboardElo = $"{Prefix}leaderboard:elo";
    
    public static string RateLimit(string userId) => $"{Prefix}ratelimit:{userId}";
}
