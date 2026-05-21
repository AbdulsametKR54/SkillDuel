using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.DTOs.Admin;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using System;
using System.Threading.Tasks;
using SkillDuel.Application.Common;

namespace SkillDuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionRepository questionRepository, IUnitOfWork unitOfWork, IQuestionService questionService)
    {
        _questionRepository = questionRepository;
        _unitOfWork = unitOfWork;
        _questionService = questionService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PaginationParams @params)
    {
        DifficultyLevel? difficulty = null;
        if (!string.IsNullOrEmpty(@params.Difficulty) && Enum.TryParse<DifficultyLevel>(@params.Difficulty, true, out var d))
        {
            difficulty = d;
        }

        QuestionType? questionType = null;
        if (!string.IsNullOrEmpty(@params.QuestionType) && Enum.TryParse<QuestionType>(@params.QuestionType, true, out var qt))
        {
            questionType = qt;
        }

        var (items, total) = await _questionRepository.GetPagedAsync(
            @params.CategoryId, 
            difficulty, 
            questionType,
            @params.Page, 
            @params.PageSize);

        return Ok(new PagedResponse<Question>(items, total, @params.Page, @params.PageSize));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(QuestionCreateRequest request)
    {
        var question = new Question
        {
            Text = request.Text,
            Options = request.Options,
            CorrectOptionIndex = request.CorrectOptionIndex,
            DifficultyLevel = request.DifficultyLevel,
            QuestionType = request.QuestionType,
            CategoryId = request.CategoryId
        };

        await _questionRepository.AddAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = question.Id }, question);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, QuestionUpdateRequest request)
    {
        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null) return NotFound();

        question.Text = request.Text;
        question.Options = request.Options;
        question.CorrectOptionIndex = request.CorrectOptionIndex;
        question.DifficultyLevel = request.DifficultyLevel;
        question.QuestionType = request.QuestionType;
        question.CategoryId = request.CategoryId;

        await _questionRepository.UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return Ok(question);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null) return NotFound();

        await _questionRepository.DeleteAsync(question);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("suggest")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Suggest([FromBody] SkillDuel.Application.DTOs.Questions.SuggestQuestionRequest request, System.Threading.CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResult("User not found."));
        }

        var result = await _questionService.SuggestQuestionAsync(userId, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
