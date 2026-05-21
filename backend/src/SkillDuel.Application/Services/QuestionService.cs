using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Questions;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SkillDuel.Application.Services;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(IQuestionRepository questionRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, ILogger<QuestionService> logger)
    {
        _questionRepository = questionRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> SuggestQuestionAsync(Guid userId, SuggestQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            return ApiResponse<bool>.FailureResult("Category not found.");
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Text = request.Text,
            Options = request.Options,
            CorrectOptionIndex = request.CorrectOptionIndex,
            DifficultyLevel = request.Difficulty,
            QuestionType = request.QuestionType,
            Status = QuestionStatus.Pending,
            CreatedByUserId = userId
        };

        await _questionRepository.AddAsync(question, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true);
    }

    public async Task<ApiResponse<List<PendingQuestionResponse>>> GetPendingQuestionsAsync(CancellationToken cancellationToken = default)
    {
        int pendingIntValue = (int)QuestionStatus.Pending;
        _logger.LogInformation("Querying for Pending questions. QuestionStatus.Pending int value: {Value}", pendingIntValue);

        var pendingQuestions = await _questionRepository.GetPendingQuestionsAsync(cancellationToken);
        
        _logger.LogInformation("Fetched {Count} pending questions from database using filter.", pendingQuestions.Count);

        var responseList = pendingQuestions.Select(q => new PendingQuestionResponse
        {
            Id = q.Id,
            Text = q.Text,
            Options = q.Options,
            CorrectOptionIndex = q.CorrectOptionIndex,
            CategoryName = q.Category?.Name ?? "Unknown",
            DifficultyLevel = q.DifficultyLevel,
            QuestionType = q.QuestionType,
            CreatedByUserId = q.CreatedByUserId,
            SubmittedByUsername = q.CreatedByUser?.Username ?? "Unknown",
            CreatedAt = q.CreatedAt
        }).ToList();

        return ApiResponse<List<PendingQuestionResponse>>.SuccessResult(responseList);
    }

    public async Task<ApiResponse<bool>> ApproveQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(questionId, cancellationToken);
        if (question == null)
        {
            return ApiResponse<bool>.FailureResult("Question not found.");
        }

        if (question.Status != QuestionStatus.Pending)
        {
            return ApiResponse<bool>.FailureResult("Question is not in pending status.");
        }

        question.Status = QuestionStatus.Approved;
        await _questionRepository.UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true);
    }

    public async Task<ApiResponse<bool>> RejectQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(questionId, cancellationToken);
        if (question == null)
        {
            return ApiResponse<bool>.FailureResult("Question not found.");
        }

        if (question.Status != QuestionStatus.Pending)
        {
            return ApiResponse<bool>.FailureResult("Question is not in pending status.");
        }

        question.Status = QuestionStatus.Rejected;
        await _questionRepository.UpdateAsync(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true);
    }
}
