
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Interfaces;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IUserService _userService;

    public LeaderboardController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 50)
    {
        var result = await _userService.GetLeaderboardAsync(limit);
        return Ok(result);
    }
}
