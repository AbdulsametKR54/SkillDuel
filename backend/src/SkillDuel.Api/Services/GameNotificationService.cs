using Microsoft.AspNetCore.SignalR;
using SkillDuel.Application.DTOs.Game;
using SkillDuel.Application.Interfaces;
using SkillDuel.Api.Hubs;
using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SkillDuel.Api.Services;

public class GameNotificationService : IGameNotificationService
{
    private readonly IHubContext<GameHub, IGameHub> _hubContext;
    private readonly IConnectionMultiplexer _redis;

    public GameNotificationService(IHubContext<GameHub, IGameHub> hubContext, IConnectionMultiplexer redis)
    {
        _hubContext = hubContext;
        _redis = redis;
    }

    public async Task SendNewQuestionAsync(Guid sessionId, QuestionDto question)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).RoundStarted(new
        {
            roundNumber = question.RoundNumber,
            question = question,
            durationSeconds = 15
        });
    }

    public async Task SendRoundResultAsync(Guid sessionId, RoundResultDto result)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).RoundResult(result);
    }

    public async Task SendGameEndedAsync(Guid sessionId, GameOverDto gameOver)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).GameEnded(gameOver);
    }

    public async Task SendGameErrorAsync(Guid sessionId, string message)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).GameError(new { message });
    }

    public async Task SendGameReadyAsync(Guid sessionId)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).GameReady();
    }

    public async Task SendMatchmakingTimeoutAsync(Guid userId)
    {
        await _hubContext.Clients.User(userId.ToString()).MatchmakingTimeout(new { message = "No opponent found. Please try again." });
    }

    public async Task SendFriendRequestNotificationAsync(Guid receiverUserId, string senderUsername)
    {
        var db = _redis.GetDatabase();
        var connId = await db.StringGetAsync($"skillduel:userconnection:{receiverUserId}");
        if (!connId.IsNull)
        {
            await _hubContext.Clients.Client(connId.ToString()).FriendRequestReceived(senderUsername);
        }
    }

    public async Task SendMatchFoundAsync(Guid userId, object data)
    {
        await _hubContext.Clients.User(userId.ToString()).MatchFound(data);
    }
}
