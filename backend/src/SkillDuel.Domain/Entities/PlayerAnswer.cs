using System;

namespace SkillDuel.Domain.Entities;

public class PlayerAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameRoundId { get; set; }
    public Guid PlayerId { get; set; }
    public int SelectedOptionIndex { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public long TimeMs { get; set; }

    // Navigation
    public virtual GameRound GameRound { get; set; } = null!;
}
