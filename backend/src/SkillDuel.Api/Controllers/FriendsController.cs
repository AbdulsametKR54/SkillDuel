using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Friendship;
using SkillDuel.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;

    public FriendsController(IFriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FriendResponse>>>> GetFriends()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetFriendsListAsync(userId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<List<FriendRequestResponse>>>> GetPendingRequests()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetPendingRequestsAsync(userId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<object>>> SendFriendRequest([FromBody] SendFriendRequestDto request)
    {
        var userId = GetUserId();
        var result = await _friendshipService.SendFriendRequestAsync(userId, request.Username);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("request/{id}/accept")]
    public async Task<ActionResult<ApiResponse<object>>> AcceptFriendRequest(Guid id)
    {
        var userId = GetUserId();
        var result = await _friendshipService.AcceptFriendRequestAsync(userId, id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("request/{id}/decline")]
    public async Task<ActionResult<ApiResponse<object>>> DeclineFriendRequest(Guid id)
    {
        var userId = GetUserId();
        var result = await _friendshipService.DeclineFriendRequestAsync(userId, id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFriend(Guid id)
    {
        var userId = GetUserId();
        var result = await _friendshipService.RemoveFriendAsync(userId, id);
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

public class SendFriendRequestDto
{
    public string Username { get; set; } = string.Empty;
}
