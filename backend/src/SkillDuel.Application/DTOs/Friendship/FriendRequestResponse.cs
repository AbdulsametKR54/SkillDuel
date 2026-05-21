using System;

namespace SkillDuel.Application.DTOs.Friendship;

public class FriendRequestResponse
{
    public Guid FriendshipId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public int SenderElo { get; set; }
    public DateTime CreatedAt { get; set; }
}
