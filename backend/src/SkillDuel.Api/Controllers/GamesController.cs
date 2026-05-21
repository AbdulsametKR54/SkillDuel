
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return Unauthorized();
        }

        var result = await _gameService.GetMatchHistoryAsync(userId, page, pageSize);
        return Ok(result);
    }
}
