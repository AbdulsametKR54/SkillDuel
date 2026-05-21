using System;

namespace SkillDuel.Domain.Entities;

public class UserCategoryStat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public int CorrectAnswersCount { get; set; }
    public int TotalAnswersCount { get; set; }
}
