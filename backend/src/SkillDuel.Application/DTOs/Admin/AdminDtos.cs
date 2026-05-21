using System;

namespace SkillDuel.Application.DTOs.Admin;

public record CategoryCreateRequest(string Name, string Slug);

public record QuestionCreateRequest(
    string Text, 
    string[] Options, 
    int CorrectOptionIndex, 
    SkillDuel.Domain.Enums.DifficultyLevel DifficultyLevel, 
    SkillDuel.Domain.Enums.QuestionType QuestionType,
    Guid CategoryId);

public record QuestionUpdateRequest(
    string Text, 
    string[] Options, 
    int CorrectOptionIndex, 
    SkillDuel.Domain.Enums.DifficultyLevel DifficultyLevel, 
    SkillDuel.Domain.Enums.QuestionType QuestionType,
    Guid CategoryId);

public record UserRoleUpdateRequest(string Role);

public record PaginationParams(Guid? CategoryId = null, string? Difficulty = null, string? QuestionType = null, int Page = 1, int PageSize = 10);

public record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
