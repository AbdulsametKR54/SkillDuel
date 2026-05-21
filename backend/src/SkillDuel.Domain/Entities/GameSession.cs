using System;
using SkillDuel.Domain.Enums;

namespace SkillDuel.Domain.Entities;

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Player1Id { get; set; }
    public Guid Player2Id { get; set; }
    public Guid? Player3Id { get; set; }
    public Guid? Player4Id { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public Guid? WinnerId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public int? Player3Score { get; set; }
    public int? Player4Score { get; set; }
    public int Player1EloChange { get; set; }
    public int Player2EloChange { get; set; }
    public int? Player3EloChange { get; set; }
    public int? Player4EloChange { get; set; }

    public User Player1 { get; set; } = null!;
    public User Player2 { get; set; } = null!;
    public User? Player3 { get; set; }
    public User? Player4 { get; set; }
}
