using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Game;
using SkillDuel.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IGameService
{
    Task StartGameAsync(Guid sessionId, GameMode mode, 
        Guid? p1CategoryId, Guid? p2CategoryId, 
        DifficultyLevel? p1Difficulty, DifficultyLevel? p2Difficulty,
        QuestionType? p1Type, QuestionType? p2Type,
        Guid p1Id, Guid p2Id,
        string p1Username, string p2Username,
        Guid? p3Id = null, Guid? p4Id = null,
        string? p3Username = null, string? p4Username = null);
    Task SubmitAnswerAsync(Guid sessionId, Guid playerId, int optionIndex, long timeMs);
    Task HandleTimeoutAsync(Guid sessionId, int roundNumber);
    Task<ApiResponse<List<MatchHistoryResponse>>> GetMatchHistoryAsync(Guid userId, int page = 1, int pageSize = 10);
    Task PlayerDisconnectedAsync(Guid sessionId, Guid playerId);
}
