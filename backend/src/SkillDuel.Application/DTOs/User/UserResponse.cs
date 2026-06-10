using System;

namespace SkillDuel.Application.DTOs.User;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int EloRating { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalGames { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}
