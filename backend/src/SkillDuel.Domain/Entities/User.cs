using System;

namespace SkillDuel.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1000;
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalGames { get; set; }
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public bool IsBanned { get; set; } = false;
    public DateTime? BanExpiresAt { get; set; }
}
