using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Questions;
using SkillDuel.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SkillDuel.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/questions")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public AdminQuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<PendingQuestionResponse>>>> GetPending(CancellationToken cancellationToken)
    {
        var result = await _questionService.GetPendingQuestionsAsync(cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{id}/approve")]
    public async Task<ActionResult<ApiResponse<bool>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _questionService.ApproveQuestionAsync(id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{id}/reject")]
    public async Task<ActionResult<ApiResponse<bool>>> Reject(Guid id, CancellationToken cancellationToken)
    {
        var result = await _questionService.RejectQuestionAsync(id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
