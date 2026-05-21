using System;
using System.Collections.Generic;

namespace SkillDuel.Application.DTOs.Game;

public record QuestionDto(
    Guid Id,
    string Text,
    string[] Options,
    int RoundNumber,
    int TotalRounds,
    int DurationSeconds,
    string CategoryName,
    string QuestionType,
    string Difficulty);

public record PlayerRoundResult(
    Guid PlayerId,
    bool IsCorrect,
    int Score,
    int CorrectCount
);

public record RoundResultDto(
    int RoundNumber,
    int? CorrectOptionIndex,
    List<PlayerRoundResult> Players
);

public record PlayerGameResult(
    Guid PlayerId,
    string Username,
    int FinalScore,
    int NewRating,
    int RatingDelta
);

public record GameOverDto(
    Guid? WinnerId,
    string? WinnerUsername,
    List<PlayerGameResult> Players
);
