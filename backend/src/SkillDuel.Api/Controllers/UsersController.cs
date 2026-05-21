using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.User;
using SkillDuel.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _userService.GetProfileAsync(userId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("me/stats")]
    public async Task<ActionResult<ApiResponse<List<UserCategoryStatResponse>>>> GetMyStats(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _userService.GetUserCategoryStatsAsync(userId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateMe(
        [FromBody] UpdateUsernameRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateUsernameAsync(userId, request.NewUsername, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User identity not found.");
        }
        return userId;
    }
}
