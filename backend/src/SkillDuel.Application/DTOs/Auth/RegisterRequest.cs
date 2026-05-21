namespace SkillDuel.Application.DTOs.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password);
