
using System;

namespace SkillDuel.Application.DTOs.User;

public class LeaderboardResponse
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int EloRating { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public double WinRate => (TotalWins + TotalLosses) == 0 ? 0 : Math.Round((double)TotalWins / (TotalWins + TotalLosses) * 100, 1);
}
