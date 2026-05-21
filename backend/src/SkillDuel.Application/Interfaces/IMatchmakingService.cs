using SkillDuel.Domain.Enums;

namespace SkillDuel.Application.Interfaces;

public interface IMatchmakingService
{
    Task JoinQueueAsync(Guid userId, GameMode mode, Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType);
    Task LeaveQueueAsync(Guid userId);
    Task<(Guid Player1, Guid Player2)?> TryMatchAsync(GameMode mode);
}
