
using System;

namespace SkillDuel.Application.DTOs.Game;

public class MatchHistoryResponse
{
    public Guid SessionId { get; set; }
    public string OpponentUsername { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // "Win", "Loss", "Draw"
    public int MyScore { get; set; }
    public int OpponentScore { get; set; }
    public int EloChange { get; set; }
    public DateTime PlayedAt { get; set; }
}
