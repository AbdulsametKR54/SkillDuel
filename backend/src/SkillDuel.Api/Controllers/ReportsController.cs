using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Common;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using System.Security.Claims;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IReportRepository reportRepository, IUnitOfWork unitOfWork)
    {
        _reportRepository = reportRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateReport([FromBody] CreateReportRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var reporterId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResult("Kullanıcı bulunamadı."));
        }

        var report = new Report
        {
            ReporterId = reporterId,
            ReportedUserId = request.ReportedUserId,
            Reason = request.Reason,
            ChatMessage = request.ChatMessage
        };

        await _reportRepository.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResult(true));
    }
}

public class CreateReportRequest
{
    public Guid ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ChatMessage { get; set; }
}
