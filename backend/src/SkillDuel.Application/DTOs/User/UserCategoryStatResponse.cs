using System;

namespace SkillDuel.Application.DTOs.User;

public class UserCategoryStatResponse
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public int CorrectAnswersCount { get; set; }
    public int TotalAnswersCount { get; set; }
    public double SuccessRate => TotalAnswersCount > 0 ? Math.Round((double)CorrectAnswersCount / TotalAnswersCount * 100, 1) : 0;
}
