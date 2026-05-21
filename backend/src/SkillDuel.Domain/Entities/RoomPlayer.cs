using System;

namespace SkillDuel.Domain.Entities;

public class RoomPlayer
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public int SlotNumber { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public virtual Room Room { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
