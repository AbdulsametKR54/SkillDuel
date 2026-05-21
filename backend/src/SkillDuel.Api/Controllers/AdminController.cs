using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.DTOs.Admin;
using SkillDuel.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminController(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        int pageSize = 10;
        var (items, totalCount) = await _userRepository.GetPagedUsersAsync(search, page, pageSize);

        var userList = items.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.Role,
            Elo = u.EloRating,
            TotalGames = u.TotalGames,
            JoinedDate = u.CreatedAt,
            Status = u.IsBanned ? "Banned" : "Active"
        });

        return Ok(new
        {
            Users = userList,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut("users/{id}/ban")]
    public async Task<IActionResult> BanUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        user.IsBanned = true;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Kullanıcı başarıyla banlandı.", UserId = user.Id });
    }

    [HttpPut("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        user.IsBanned = false;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Kullanıcı banı kaldırıldı.", UserId = user.Id });
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        user.Role = request.Role;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = $"Kullanıcı rolü '{request.Role}' olarak güncellendi.", UserId = user.Id });
    }
}
