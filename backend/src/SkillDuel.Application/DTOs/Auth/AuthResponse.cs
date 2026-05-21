using System;

namespace SkillDuel.Application.DTOs.Auth;

public record AuthResponse(
    Guid UserId,
    string Username,
    string Email,
    string Token,
    string RefreshToken,
    int EloRating);
