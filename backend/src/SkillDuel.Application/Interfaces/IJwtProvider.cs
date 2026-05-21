using SkillDuel.Domain.Entities;
using System.Security.Claims;

namespace SkillDuel.Application.Interfaces;

public interface IJwtProvider
{
    string Generate(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
