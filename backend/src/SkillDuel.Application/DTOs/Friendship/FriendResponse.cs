using System;

namespace SkillDuel.Application.DTOs.Friendship;

public class FriendResponse
{
    public Guid FriendshipId { get; set; }
    public Guid FriendId { get; set; }
    public string FriendUsername { get; set; } = string.Empty;
    public int FriendElo { get; set; }
    public bool IsOnline { get; set; }
}
