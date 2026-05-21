using System;

namespace SkillDuel.Application.DTOs.Game;

public record MatchFoundDto(
    Guid SessionId,
    string OpponentUsername);
