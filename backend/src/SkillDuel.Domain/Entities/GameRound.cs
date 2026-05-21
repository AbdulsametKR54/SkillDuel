using System;

namespace SkillDuel.Domain.Entities;

public class GameRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameSessionId { get; set; }
    public Guid QuestionId { get; set; }
    public int RoundNumber { get; set; }

    // Navigation
    public virtual GameSession GameSession { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}
