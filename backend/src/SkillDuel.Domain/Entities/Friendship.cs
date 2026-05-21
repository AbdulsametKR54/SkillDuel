using System;
using SkillDuel.Domain.Enums;

namespace SkillDuel.Domain.Entities;

public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActionAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual User Friend { get; set; } = null!;
}
