using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillDuel.Application.Common;
using SkillDuel.Application.Interfaces;

namespace SkillDuel.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/bans")]
public class AdminBansController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminBansController(IReportRepository reportRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _reportRepository = reportRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<object>>> GetReports()
    {
        var reports = await _reportRepository.Query()
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Where(r => !r.IsResolved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                ReporterId = r.ReporterId,
                ReporterUsername = r.Reporter.Username,
                ReportedUserId = r.ReportedUserId,
                ReportedUsername = r.ReportedUser.Username,
                r.Reason,
                r.ChatMessage,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResult(reports));
    }

    [HttpPost("ban")]
    public async Task<ActionResult<ApiResponse<bool>>> BanUser([FromBody] BanUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null) return NotFound(ApiResponse<bool>.FailureResult("Kullanıcı bulunamadı."));

        user.IsBanned = true;

        switch (request.Duration)
        {
            case "1h":
                user.BanExpiresAt = DateTime.UtcNow.AddHours(1);
                break;
            case "1d":
                user.BanExpiresAt = DateTime.UtcNow.AddDays(1);
                break;
            case "1m":
                user.BanExpiresAt = DateTime.UtcNow.AddMonths(1);
                break;
            case "perm":
                user.BanExpiresAt = null;
                break;
            default:
                return BadRequest(ApiResponse<bool>.FailureResult("Geçersiz ban süresi."));
        }

        await _userRepository.UpdateAsync(user);

        // Resolve all active reports for this user automatically
        var reports = await _reportRepository.Query()
            .Where(r => r.ReportedUserId == request.UserId && !r.IsResolved)
            .ToListAsync();

        foreach (var report in reports)
        {
            report.IsResolved = true;
            await _reportRepository.UpdateAsync(report);
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResult(true));
    }

    [HttpPost("reports/{id}/resolve")]
    public async Task<ActionResult<ApiResponse<bool>>> ResolveReport(Guid id)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null) return NotFound(ApiResponse<bool>.FailureResult("Rapor bulunamadı."));

        report.IsResolved = true;
        await _reportRepository.UpdateAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResult(true));
    }
}

public class BanUserRequest
{
    public Guid UserId { get; set; }
    public string Duration { get; set; } = string.Empty; // 1h, 1d, 1m, perm
}
