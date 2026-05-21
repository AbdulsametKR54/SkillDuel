using System;
using SkillDuel.Domain.Enums;

namespace SkillDuel.Domain.Entities;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // 6-char unique
    public string Name { get; set; } = string.Empty;
    public Guid HostId { get; set; }
    public Guid? GuestId { get; set; }
    public bool IsPrivate { get; set; }
    public string? Password { get; set; } // Hashed
    public Guid? CategoryId { get; set; }
    public string? Difficulty { get; set; }
    public string? QuestionType { get; set; }
    public int RoundCount { get; set; }
    public int MaxPlayers { get; set; } = 2;
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);

    // Navigation properties
    public virtual User Host { get; set; } = null!;
    public virtual User? Guest { get; set; }
    public virtual Category? Category { get; set; }
    public virtual ICollection<RoomPlayer> Players { get; set; } = new List<RoomPlayer>();
}
