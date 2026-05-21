using SkillDuel.Domain.Enums;
using System;

namespace SkillDuel.Application.DTOs.Questions;

public class PendingQuestionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
    public int CorrectOptionIndex { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DifficultyLevel DifficultyLevel { get; set; }
    public QuestionType QuestionType { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string SubmittedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
