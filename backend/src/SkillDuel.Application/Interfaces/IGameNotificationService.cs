using SkillDuel.Application.DTOs.Game;
using System;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IGameNotificationService
{
    Task SendNewQuestionAsync(Guid sessionId, QuestionDto question);
    Task SendRoundResultAsync(Guid sessionId, RoundResultDto result);
    Task SendGameEndedAsync(Guid sessionId, GameOverDto gameOver);
    Task SendGameErrorAsync(Guid sessionId, string message);
    Task SendGameReadyAsync(Guid sessionId);
    Task SendMatchmakingTimeoutAsync(Guid userId);
    Task SendFriendRequestNotificationAsync(Guid receiverUserId, string senderUsername);
    Task SendMatchFoundAsync(Guid userId, object data);
}
