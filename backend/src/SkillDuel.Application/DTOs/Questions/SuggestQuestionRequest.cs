using SkillDuel.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SkillDuel.Application.DTOs.Questions;

public class SuggestQuestionRequest
{
    [Required]
    public string Text { get; set; } = string.Empty;
    
    [Required]
    public string[] Options { get; set; } = Array.Empty<string>();
    
    public int CorrectOptionIndex { get; set; }
    
    [Required]
    public Guid CategoryId { get; set; }
    
    public DifficultyLevel Difficulty { get; set; }
    
    public QuestionType QuestionType { get; set; }
}
