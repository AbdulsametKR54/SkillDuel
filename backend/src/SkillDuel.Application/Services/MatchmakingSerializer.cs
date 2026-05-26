using System;

namespace SkillDuel.Application.Services;

public static class MatchmakingSerializer
{
    public static string Serialize(Guid userId, Guid categoryId, int difficulty, int type, int elo, long joinedAt)
    {
        return $"{userId}:{categoryId}:{difficulty}:{type}:{elo}:{joinedAt}";
    }

    public static MatchmakingMember? Deserialize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var parts = raw.Split(':');
        if (parts.Length < 6) return null;

        return new MatchmakingMember
        {
            UserId = Guid.Parse(parts[0]),
            CategoryId = string.IsNullOrEmpty(parts[1]) ? Guid.Empty : Guid.Parse(parts[1]),
            Difficulty = int.Parse(parts[2]),
            QuestionType = int.Parse(parts[3]),
            Elo = int.Parse(parts[4]),
            JoinedAt = long.Parse(parts[5]),
            RawValue = raw
        };
    }
}

public class MatchmakingMember
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public int Difficulty { get; set; }
    public int QuestionType { get; set; }
    public int Elo { get; set; }
    public long JoinedAt { get; set; }
    public string RawValue { get; set; } = string.Empty;
}
