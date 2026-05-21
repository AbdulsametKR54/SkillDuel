using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Questions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IQuestionService
{
    Task<ApiResponse<bool>> SuggestQuestionAsync(Guid userId, SuggestQuestionRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<PendingQuestionResponse>>> GetPendingQuestionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ApproveQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RejectQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);
}
