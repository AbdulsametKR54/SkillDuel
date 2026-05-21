using System;
using System.Collections.Generic;
using SkillDuel.Domain.Enums;

namespace SkillDuel.Domain.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
    public int CorrectOptionIndex { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public QuestionType QuestionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public QuestionStatus Status { get; set; } = QuestionStatus.Approved;
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
}
