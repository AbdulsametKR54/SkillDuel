using FluentValidation;
using SkillDuel.Application.DTOs.Admin;

namespace SkillDuel.Application.Validators.Admin;

public class QuestionCreateRequestValidator : AbstractValidator<QuestionCreateRequest>
{
    public QuestionCreateRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Options).NotNull().Must(x => x.Length == 4)
            .WithMessage("Soru tam olarak 4 seçenek içermelidir.");
        RuleFor(x => x.CorrectOptionIndex).InclusiveBetween(0, 3)
            .WithMessage("Doğru cevap indeksi 0 ile 3 arasında olmalıdır.");
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class QuestionUpdateRequestValidator : AbstractValidator<QuestionUpdateRequest>
{
    public QuestionUpdateRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Options).NotNull().Must(x => x.Length == 4)
            .WithMessage("Soru tam olarak 4 seçenek içermelidir.");
        RuleFor(x => x.CorrectOptionIndex).InclusiveBetween(0, 3)
            .WithMessage("Doğru cevap indeksi 0 ile 3 arasında olmalıdır.");
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class CategoryCreateRequestValidator : AbstractValidator<CategoryCreateRequest>
{
    public CategoryCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
    }
}
