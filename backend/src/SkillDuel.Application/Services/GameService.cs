using StackExchange.Redis;
using SkillDuel.Application.Common;
using SkillDuel.Application.Interfaces;
using SkillDuel.Application.DTOs.Game;
using SkillDuel.Domain.Constants;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Domain.Utils;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Application.Services;

public class GameService : IGameService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUserCategoryStatRepository _userCategoryStatRepository;
    private readonly IGameNotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<GameService> _logger;
    private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

    public GameService(
        IConnectionMultiplexer redis,
        IUnitOfWork unitOfWork,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        IGameSessionRepository gameSessionRepository,
        IRoomRepository roomRepository,
        ICategoryRepository categoryRepository,
        IUserCategoryStatRepository userCategoryStatRepository,
        IGameNotificationService notificationService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<GameService> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _unitOfWork = unitOfWork;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _gameSessionRepository = gameSessionRepository;
        _roomRepository = roomRepository;
        _categoryRepository = categoryRepository;
        _userCategoryStatRepository = userCategoryStatRepository;
        _notificationService = notificationService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task StartGameAsync(Guid sessionId, GameMode mode, 
        Guid? p1CategoryId, Guid? p2CategoryId, 
        DifficultyLevel? p1Difficulty, DifficultyLevel? p2Difficulty,
        QuestionType? p1Type, QuestionType? p2Type,
        Guid p1Id, Guid p2Id,
        string p1Username, string p2Username,
        Guid? p3Id = null, Guid? p4Id = null,
        string? p3Username = null, string? p4Username = null)
    {
        _logger.LogInformation("StartGameAsync called for Session {SessionId}", sessionId);
        var startTime = DateTime.UtcNow;
        
        int totalRounds = (int)mode;
        bool identicalPrefs = p1CategoryId == p2CategoryId && p1Difficulty == p2Difficulty && p1Type == p2Type;
        
        // 1. Fetch approved DB questions matching player preferences
        List<Question> dbQuestions;
        if (identicalPrefs)
        {
            dbQuestions = await _questionRepository.GetRandomQuestionsAsync(totalRounds, p1CategoryId, p1Difficulty, p1Type);
        }
        else
        {
            var q1 = await _questionRepository.GetRandomQuestionsAsync(totalRounds, p1CategoryId, p1Difficulty, p1Type);
            var q2 = await _questionRepository.GetRandomQuestionsAsync(totalRounds, p2CategoryId, p2Difficulty, p2Type);
            // Combine and deduplicate by text
            dbQuestions = q1.Concat(q2).GroupBy(q => q.Text).Select(g => g.First()).ToList();
        }

        int dbCountUsed = Math.Min(dbQuestions.Count, totalRounds);
        var questions = dbQuestions.OrderBy(_ => Guid.NewGuid()).Take(dbCountUsed).ToList();
        int apiCountUsed = 0;

        // 2. If DB matches < totalRounds, fetch remaining from OpenTDB API
        if (questions.Count < totalRounds)
        {
            int neededFromApi = totalRounds - questions.Count;
            _logger.LogInformation("Session {SessionId}: DB had {Count}/{Total}. Fetching remainder from API.", sessionId, questions.Count, totalRounds);

            if (identicalPrefs)
            {
                var apiQs = await FetchQuestionsFromOpenTdbAsync(neededFromApi, p1CategoryId, p1Difficulty, p1Type);
                foreach (var q in apiQs)
                {
                    if (questions.Count < totalRounds && !questions.Any(e => e.Text == q.Text))
                    {
                        questions.Add(q);
                        apiCountUsed++;
                    }
                }
            }
            else
            {
                int p1ApiNeeded = neededFromApi / 2;
                int p2ApiNeeded = totalRounds - questions.Count - p1ApiNeeded;

                if (p1ApiNeeded > 0)
                {
                    var api1 = await FetchQuestionsFromOpenTdbAsync(p1ApiNeeded, p1CategoryId, p1Difficulty, p1Type);
                    foreach (var q in api1)
                    {
                        if (questions.Count < totalRounds && !questions.Any(e => e.Text == q.Text))
                        {
                            questions.Add(q);
                            apiCountUsed++;
                        }
                    }
                }

                if (questions.Count < totalRounds)
                {
                    // OpenTDB rate limit: 1 request per 5 seconds. Delay if we already made a call.
                    if (p1ApiNeeded > 0) await Task.Delay(5000);

                    int p2RealNeeded = totalRounds - questions.Count;
                    var api2 = await FetchQuestionsFromOpenTdbAsync(p2RealNeeded, p2CategoryId, p2Difficulty, p2Type);
                    foreach (var q in api2)
                    {
                        if (questions.Count < totalRounds && !questions.Any(e => e.Text == q.Text))
                        {
                            questions.Add(q);
                            apiCountUsed++;
                        }
                    }
                }
            }
        }

        // 3. Final check for question count
        if (questions.Count < totalRounds)
        {
            _logger.LogWarning("Still short of questions ({Have}/{Need}). Attempting one last fetch of pure random from DB.", questions.Count, totalRounds);
            var pureRandom = await _questionRepository.GetRandomQuestionsAsync(totalRounds - questions.Count, null, null, null);
            foreach (var q in pureRandom)
            {
                if (questions.Count < totalRounds && !questions.Any(e => e.Text == q.Text))
                {
                    questions.Add(q);
                    dbCountUsed++;
                }
            }
        }

        _logger.LogInformation("Questions for session {sessionId}: {dbCount} from DB, {apiCount} from API", sessionId, dbCountUsed, apiCountUsed);

        var stateKey = RedisKeys.GameState(sessionId.ToString());

        if (questions.Count < totalRounds)
        {
            _logger.LogError("CRITICAL: Not enough questions found for Session {SessionId}. (Found {Have}, Need {Need}). Aborting game.", 
                sessionId, questions.Count, totalRounds);
            
            await _notificationService.SendGameErrorAsync(sessionId, "Not enough questions available. Please try again.");
            
            var session = await _gameSessionRepository.GetByIdAsync(sessionId);
            if (session != null)
            {
                session.Status = GameStatus.Finished;
                session.EndedAt = DateTime.UtcNow;
                await _gameSessionRepository.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();
            }
            
            await _db.KeyDeleteAsync(stateKey);
            return;
        }

        // 4. Merge and shuffle final list
        questions = questions.OrderBy(_ => Guid.NewGuid()).ToList();

        await _notificationService.SendGameReadyAsync(sessionId);
        await Task.Delay(3000);

        var hashEntries = new List<HashEntry>
        {
            new("CurrentRound", 1),
            new("TotalRounds", totalRounds),
            new("P1Score", 0),
            new("P2Score", 0),
            new("P1CorrectCount", 0),
            new("P2CorrectCount", 0),
            new("P1Id", p1Id.ToString()),
            new("P2Id", p2Id.ToString()),
            new("P1Username", p1Username),
            new("P2Username", p2Username)
        };
        
        int playerCount = 2;
        if (p3Id.HasValue) {
            playerCount++;
            hashEntries.Add(new("P3Score", 0));
            hashEntries.Add(new("P3CorrectCount", 0));
            hashEntries.Add(new("P3Id", p3Id.Value.ToString()));
            hashEntries.Add(new("P3Username", p3Username!));
        }
        if (p4Id.HasValue) {
            playerCount++;
            hashEntries.Add(new("P4Score", 0));
            hashEntries.Add(new("P4CorrectCount", 0));
            hashEntries.Add(new("P4Id", p4Id.Value.ToString()));
            hashEntries.Add(new("P4Username", p4Username!));
        }
        hashEntries.Add(new("PlayerCount", playerCount));

        for (int i = 0; i < questions.Count; i++)
        {
            var simplifiedQuestion = new 
            {
                Id = questions[i].Id,
                Text = questions[i].Text,
                Options = questions[i].Options,
                CorrectOptionIndex = questions[i].CorrectOptionIndex,
                QuestionType = questions[i].QuestionType,
                DifficultyLevel = questions[i].DifficultyLevel,
                CategoryId = questions[i].CategoryId,
                CategoryName = questions[i].Category?.Name
            };

            var qJson = System.Text.Json.JsonSerializer.Serialize(simplifiedQuestion);
            hashEntries.Add(new($"Q{i + 1}_Data", qJson));
            hashEntries.Add(new($"Q{i + 1}_Correct", questions[i].CorrectOptionIndex));
        }

        await _db.HashSetAsync(stateKey, hashEntries.ToArray());
        await _db.KeyExpireAsync(stateKey, TimeSpan.FromHours(1));

        _logger.LogInformation("Game session {SessionId} initialized with {RoundCount} rounds. Starting first round.", sessionId, totalRounds);
        await SendRoundQuestionAsync(sessionId, 1);
    }

    private async Task<List<Question>> FetchQuestionsFromOpenTdbAsync(
        int amount, Guid? categoryId, DifficultyLevel? difficulty, QuestionType? type)
    {
        var questions = new List<Question>();
        if (amount <= 0) return questions;
        
        string categoryParam = string.Empty;
        if (categoryId.HasValue && _categoryRepository != null)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
            if (category != null)
            {
                int? tdbId = category.OpenTdbId ?? GetOpenTdbCategoryIdFallback(category.Name);
                if (tdbId.HasValue)
                {
                    categoryParam = $"&category={tdbId.Value}";
                }
            }
        }

        string difficultyParam = string.Empty;
        if (difficulty.HasValue)
        {
            difficultyParam = difficulty.Value switch
            {
                DifficultyLevel.Easy => "&difficulty=easy",
                DifficultyLevel.Medium => "&difficulty=medium",
                DifficultyLevel.Hard => "&difficulty=hard",
                _ => string.Empty
            };
        }

        string typeParam = string.Empty;
        if (type.HasValue)
        {
            typeParam = type.Value == QuestionType.TrueFalse ? "&type=boolean" : "&type=multiple";
        }

        var url = $"https://opentdb.com/api.php?amount={amount}{categoryParam}{difficultyParam}{typeParam}";
        _logger.LogInformation("Constructed OpenTDB URL: {Url}", url);
        
        var cacheKey = $"TDB_CACHE_{amount}_{categoryParam}_{difficultyParam}_{typeParam}";
        var cachedResponse = await _db.StringGetAsync(cacheKey);

        string responseJson;
        if (!cachedResponse.IsNullOrEmpty)
        {
            responseJson = cachedResponse;
            _logger.LogInformation("Fetched {Amount} questions from Redis cache for URL: {Url}", amount, url);
        }
        else
        {
            try
            {
                responseJson = await _httpClient.GetStringAsync(url);
                await _db.StringSetAsync(cacheKey, responseJson, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Fetched {Amount} questions from OpenTDB API for URL: {Url}", amount, url);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error fetching from OpenTDB API: {Url}", url);
                return questions;
            }
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<OpenTdbResponse>(responseJson);
        if (result?.Results == null || !result.Results.Any()) return questions;

        var random = new Random();
        foreach (var r in result.Results)
        {
            var options = new List<string>();
            int correctIndex = 0;

            if (r.Type == "boolean")
            {
                options.Add("True");
                options.Add("False");
                correctIndex = r.CorrectAnswer == "True" ? 0 : 1;
            }
            else
            {
                foreach(var incorrectAnswer in r.IncorrectAnswers)
                {
                    options.Add(System.Net.WebUtility.HtmlDecode(incorrectAnswer));
                }
                
                options = options.OrderBy(x => random.Next()).ToList();
                correctIndex = random.Next(options.Count + 1);
                var decodedCorrectAnswer = System.Net.WebUtility.HtmlDecode(r.CorrectAnswer);
                options.Insert(correctIndex, decodedCorrectAnswer);
            }

            var qCategory = new Category { Id = Guid.Empty, Name = System.Net.WebUtility.HtmlDecode(r.Category), Slug = "" };
            
            var question = new Question
            {
                Id = Guid.NewGuid(),
                Text = System.Net.WebUtility.HtmlDecode(r.Question),
                Options = options.ToArray(),
                CorrectOptionIndex = correctIndex,
                QuestionType = r.Type == "boolean" ? QuestionType.TrueFalse : QuestionType.Multiple,
                DifficultyLevel = r.Difficulty.ToLower() switch
                {
                    "easy" => DifficultyLevel.Easy,
                    "medium" => DifficultyLevel.Medium,
                    "hard" => DifficultyLevel.Hard,
                    _ => DifficultyLevel.Medium
                },
                Category = qCategory,
                CategoryId = Guid.Empty
            };

            questions.Add(question);
        }

        return questions;
    }

    public async Task SubmitAnswerAsync(Guid sessionId, Guid playerId, int optionIndex, long timeMs)
    {
        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var state = await _db.HashGetAllAsync(stateKey);
        if (state.Length == 0) return;

        int currentRound = (int)state.First(x => x.Name == "CurrentRound").Value;
        string pid = playerId.ToString();
        string playerSuffix = pid == state.FirstOrDefault(x => x.Name == "P1Id").Value ? "P1" :
                              (pid == state.FirstOrDefault(x => x.Name == "P2Id").Value ? "P2" :
                              (pid == state.FirstOrDefault(x => x.Name == "P3Id").Value ? "P3" : "P4"));
        
        _logger.LogInformation("[Game] Session {SessionId}: Player {PlayerId} ({Suffix}) submitted option {Option} in {TimeMs}ms (Round {Round})", 
            sessionId, playerId, playerSuffix, optionIndex, timeMs, currentRound);

        if (state.Any(x => x.Name == $"{playerSuffix}_R{currentRound}_Answered")) return;

        await _db.HashSetAsync(stateKey, new HashEntry[]
        {
            new($"{playerSuffix}_R{currentRound}_Answered", true),
            new($"{playerSuffix}_R{currentRound}_Option", optionIndex),
            new($"{playerSuffix}_R{currentRound}_TimeMs", timeMs)
        });

        var newState = await _db.HashGetAllAsync(stateKey);
        int playerCount = (int)state.First(x => x.Name == "PlayerCount").Value;
        int answersCount = newState.Count(x => x.Name.ToString().EndsWith($"_R{currentRound}_Answered"));

        if (answersCount == playerCount)
        {
            _logger.LogInformation("[Game] Session {SessionId}: All players answered Round {Round}. Evaluating...", sessionId, currentRound);
            await EvaluateRoundAsync(sessionId, currentRound);
        }
    }

    public async Task PlayerDisconnectedAsync(Guid sessionId, Guid playerId)
    {
        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var state = await _db.HashGetAllAsync(stateKey);
        if (state.Length == 0) return;

        var countEntry = state.FirstOrDefault(x => x.Name == "PlayerCount");
        if (countEntry.Value.IsNull) return;

        int playerCount = (int)countEntry.Value;
        if (playerCount <= 1) return;

        // Decrement player count
        int newPlayerCount = playerCount - 1;
        await _db.HashSetAsync(stateKey, new HashEntry[] { new("PlayerCount", newPlayerCount) });

        if (newPlayerCount <= 1)
        {
            _logger.LogInformation("[Game] Session {SessionId}: Player dropped, only 1 player remaining. Ending game.", sessionId);
            await EndGameAsync(sessionId);
            return;
        }

        // Check if remaining players have all answered the current round
        int currentRound = (int)state.First(x => x.Name == "CurrentRound").Value;
        var newState = await _db.HashGetAllAsync(stateKey);
        int answersCount = newState.Count(x => x.Name.ToString().EndsWith($"_R{currentRound}_Answered"));

        if (answersCount >= newPlayerCount)
        {
            _logger.LogInformation("[Game] Session {SessionId}: Player dropped, remaining {NewCount} players already answered Round {Round}. Evaluating...", sessionId, newPlayerCount, currentRound);
            await EvaluateRoundAsync(sessionId, currentRound);
        }
    }

    public async Task HandleTimeoutAsync(Guid sessionId, int roundNumber)
    {
        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var state = await _db.HashGetAllAsync(stateKey);
        if (state.Length == 0) return;

        int currentRound = (int)state.First(x => x.Name == "CurrentRound").Value;
        if (currentRound != roundNumber) return;

        await EvaluateRoundAsync(sessionId, currentRound);
    }
    
    private async Task EvaluateRoundAsync(Guid sessionId, int roundNumber)
    {
        string lockKey = $"RoundEvaluated:{sessionId}:{roundNumber}";
        if (!await _db.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(30), When.NotExists))
        {
            _logger.LogWarning("[Game] Session {SessionId}: Round {Round} already being evaluated. Skipping.", sessionId, roundNumber);
            return;
        }

        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var state = await _db.HashGetAllAsync(stateKey);
        
        int correctIndex = (int)state.First(x => x.Name == $"Q{roundNumber}_Correct").Value;
        int playerCount = (int)state.First(x => x.Name == "PlayerCount").Value;

        var playerResults = new List<PlayerRoundResult>();
        var correctPlayers = new List<(string Suffix, Guid Id, long Time)>();
        var hashUpdates = new List<HashEntry>();

        string[] suffixes = { "P1", "P2", "P3", "P4" };
        
        for (int i = 0; i < suffixes.Length; i++)
        {
            string suffix = suffixes[i];
            var idEntry = state.FirstOrDefault(x => x.Name == $"{suffix}Id");
            if (idEntry.Value.IsNull) continue;

            Guid pid = Guid.Parse(idEntry.Value!);
            
            var ans = state.FirstOrDefault(x => x.Name == $"{suffix}_R{roundNumber}_Option");
            var time = state.FirstOrDefault(x => x.Name == $"{suffix}_R{roundNumber}_TimeMs");

            bool isCorrect = !ans.Value.IsNull && (int)ans.Value == correctIndex;
            if (isCorrect) correctPlayers.Add((suffix, pid, (long)time.Value));
        }

        // Update UserCategoryStats
        Guid categoryId = Guid.Empty;
        var qJsonStr = (string?)state.FirstOrDefault(x => x.Name == $"Q{roundNumber}_Data").Value;
        if (!string.IsNullOrEmpty(qJsonStr))
        {
            try
            {
                var question = System.Text.Json.JsonSerializer.Deserialize<Question>(qJsonStr);
                if (question != null)
                {
                    categoryId = question.CategoryId;
                    if (categoryId == Guid.Empty && question.Category != null)
                    {
                        var categories = await _categoryRepository.GetAllAsync();
                        var existingCat = categories.FirstOrDefault(c => c.Name.Equals(question.Category.Name, StringComparison.OrdinalIgnoreCase));
                        if (existingCat != null)
                        {
                            categoryId = existingCat.Id;
                        }
                        else
                        {
                            var newCat = new Category 
                            { 
                                Name = question.Category.Name, 
                                Slug = question.Category.Name.ToLower().Replace(" ", "-") 
                            };
                            await _categoryRepository.AddAsync(newCat);
                            await _unitOfWork.SaveChangesAsync();
                            categoryId = newCat.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse question data for category stats tracking.");
            }
        }

        if (categoryId != Guid.Empty)
        {
            for (int i = 0; i < suffixes.Length; i++)
            {
                string suffix = suffixes[i];
                var idEntry = state.FirstOrDefault(x => x.Name == $"{suffix}Id");
                if (idEntry.Value.IsNull) continue;

                Guid pid = Guid.Parse(idEntry.Value!);
                var ans = state.FirstOrDefault(x => x.Name == $"{suffix}_R{roundNumber}_Option");
                bool isCorrect = !ans.Value.IsNull && (int)ans.Value == correctIndex;

                var stat = await _userCategoryStatRepository.GetByUserAndCategoryAsync(pid, categoryId);
                if (stat == null)
                {
                    stat = new UserCategoryStat
                    {
                        UserId = pid,
                        CategoryId = categoryId,
                        CorrectAnswersCount = isCorrect ? 1 : 0,
                        TotalAnswersCount = 1
                    };
                    await _userCategoryStatRepository.AddAsync(stat);
                }
                else
                {
                    stat.TotalAnswersCount++;
                    if (isCorrect) stat.CorrectAnswersCount++;
                    await _userCategoryStatRepository.UpdateAsync(stat);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        // Assign scores
        correctPlayers = correctPlayers.OrderBy(p => p.Time).ToList();
        int[] scoreTiers = { 100, 75, 50, 25 };

        for (int i = 0; i < suffixes.Length; i++)
        {
            string suffix = suffixes[i];
            var idEntry = state.FirstOrDefault(x => x.Name == $"{suffix}Id");
            if (idEntry.Value.IsNull) continue;

            Guid pid = Guid.Parse(idEntry.Value!);
            
            int roundScore = 0;
            var correctEntry = correctPlayers.Select((p, idx) => new { p.Suffix, Rank = idx }).FirstOrDefault(p => p.Suffix == suffix);
            if (correctEntry != null)
            {
                // Find matching score tier
                int scoreIdx = correctEntry.Rank < scoreTiers.Length ? correctEntry.Rank : scoreTiers.Length - 1;
                roundScore = scoreTiers[scoreIdx];
                // Adjust if only one person correct, just give 100
                if (correctPlayers.Count == 1) roundScore = 100;
                else if (correctPlayers.Count == 2) {
                   roundScore = correctEntry.Rank == 0 ? 70 : 30; // Legacy 2-player score compatibility
                }
            }

            int oldScore = (int)state.First(x => x.Name == $"{suffix}Score").Value;
            int totalScore = oldScore + roundScore;
            int oldCount = (int)state.First(x => x.Name == $"{suffix}CorrectCount").Value;
            int newCount = oldCount + (correctEntry != null ? 1 : 0);

            hashUpdates.Add(new($"{suffix}Score", totalScore));
            hashUpdates.Add(new($"{suffix}CorrectCount", newCount));

            playerResults.Add(new PlayerRoundResult(pid, correctEntry != null, totalScore, newCount));
        }

        await _db.HashSetAsync(stateKey, hashUpdates.ToArray());

        await _notificationService.SendRoundResultAsync(sessionId, new RoundResultDto(
            roundNumber, correctIndex, playerResults));

        int totalRounds = (int)state.First(x => x.Name == "TotalRounds").Value;
        if (roundNumber < totalRounds)
        {
            await Task.Delay(1500); 
            int nextRound = roundNumber + 1;
            await _db.HashSetAsync(stateKey, new HashEntry[] { new("CurrentRound", nextRound) });
            await SendRoundQuestionAsync(sessionId, nextRound);
        }
        else
        {
            await EndGameAsync(sessionId);
        }
    }

    private async Task SendRoundQuestionAsync(Guid sessionId, int roundNumber)
    {
        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var qJsonStr = (string?)await _db.HashGetAsync(stateKey, $"Q{roundNumber}_Data");
        if (string.IsNullOrEmpty(qJsonStr)) return;

        var question = System.Text.Json.JsonSerializer.Deserialize<Question>(qJsonStr);
        if (question == null) return;

        int totalRounds = (int)await _db.HashGetAsync(stateKey, "TotalRounds");

        _logger.LogInformation("Sending question for Session {SessionId}, Round {RoundNumber}", sessionId, roundNumber);
        
        string typeStr = question.QuestionType == QuestionType.TrueFalse ? "True / False" : "Multiple Choice";
        
        await _notificationService.SendNewQuestionAsync(sessionId, new QuestionDto(
            question.Id, 
            question.Text, 
            question.Options, 
            roundNumber, 
            totalRounds, 
            15,
            question.Category?.Name ?? "General",
            typeStr,
            question.DifficultyLevel.ToString()));

        _logger.LogInformation("Scheduling timeout job for Session {SessionId}, Round {RoundNumber} in 15s", sessionId, roundNumber);
        _backgroundJobClient.Schedule(() => HandleTimeoutAsync(sessionId, roundNumber), TimeSpan.FromSeconds(15));
    }

    private async Task EndGameAsync(Guid sessionId)
    {
        var stateKey = RedisKeys.GameState(sessionId.ToString());
        var state = await _db.HashGetAllAsync(stateKey);
        
        int playerCount = (int)state.First(x => x.Name == "PlayerCount").Value;
        int totalRounds = (int)state.First(x => x.Name == "TotalRounds").Value;

        var session = await _gameSessionRepository.GetByIdAsync(sessionId);
        if (session == null) return;

        string[] suffixes = { "P1", "P2", "P3", "P4" };
        var playersData = new List<(string Suffix, Guid Id, string Username, int Score, User User)>();

        for (int i = 0; i < suffixes.Length; i++)
        {
            string suffix = suffixes[i];
            var idEntry = state.FirstOrDefault(x => x.Name == $"{suffix}Id");
            if (idEntry.Value.IsNull) continue;

            Guid pid = Guid.Parse(idEntry.Value!);
            string username = state.First(x => x.Name == $"{suffix}Username").Value!;
            int score = (int)state.First(x => x.Name == $"{suffix}Score").Value;
            var user = await _userRepository.GetByIdAsync(pid);
            if (user != null) playersData.Add((suffix, pid, username, score, user));
        }

        var sortedByScore = playersData.OrderByDescending(p => p.Score).ToList();
        var winner = sortedByScore.First();
        
        // Multi-player Elo calculation is complex.
        // We will just do sequential 1v1 Elo updates where every player plays against every other player.
        // But to keep it simple and avoid massive inflation, we'll just apply standard 1v1 to the top 2 if it's a 2 player game.
        // For >2 players, we will simplify: Top half gains Elo, bottom half loses Elo.
        
        int kFactor = totalRounds <= 5 ? 32 : 48;
        var newElos = new Dictionary<Guid, int>();
        var eloDeltas = new Dictionary<Guid, int>();
        
        int originalPlayerCount = playersData.Count;

        if (originalPlayerCount <= 2)
        {
            var p1 = playersData[0].User;
            var p2 = playersData[1].User;
            double p1Actual = winner.Id == p1.Id ? 1 : (playersData[0].Score == playersData[1].Score ? 0.5 : 0);
            double p2Actual = 1 - p1Actual;

            int p1Old = p1.EloRating;
            int p2Old = p2.EloRating;

            p1.EloRating = EloCalculator.CalculateNewRating(p1Old, p2Old, p1Actual, kFactor);
            p2.EloRating = EloCalculator.CalculateNewRating(p2Old, p1Old, p2Actual, kFactor);

            newElos[p1.Id] = p1.EloRating;
            newElos[p2.Id] = p2.EloRating;
            eloDeltas[p1.Id] = p1.EloRating - p1Old;
            eloDeltas[p2.Id] = p2.EloRating - p2Old;
        }
        else
        {
            // Simple generic Multi-player Elo: Each player plays a 1v1 against everyone else.
            var tempElos = playersData.ToDictionary(p => p.Id, p => p.User.EloRating);
            foreach (var p1 in playersData)
            {
                int oldElo = p1.User.EloRating;
                int ratingChange = 0;
                foreach (var p2 in playersData)
                {
                    if (p1.Id == p2.Id) continue;
                    double actual = p1.Score > p2.Score ? 1 : (p1.Score == p2.Score ? 0.5 : 0);
                    int mockNewElo = EloCalculator.CalculateNewRating(oldElo, p2.User.EloRating, actual, kFactor / (originalPlayerCount - 1));
                    ratingChange += (mockNewElo - oldElo);
                }
                p1.User.EloRating += ratingChange;
                newElos[p1.Id] = p1.User.EloRating;
                eloDeltas[p1.Id] = ratingChange;
            }
        }

        // Update DB
        session.Status = GameStatus.Finished;
        session.WinnerId = winner.Score > sortedByScore.Skip(1).First().Score ? winner.Id : null;
        session.EndedAt = DateTime.UtcNow;

        session.Player1Score = playersData[0].Score;
        session.Player2Score = playersData[1].Score;
        session.Player1EloChange = eloDeltas[playersData[0].Id];
        session.Player2EloChange = eloDeltas[playersData[1].Id];

        if (playerCount > 2) {
            session.Player3Score = playersData[2].Score;
            session.Player3EloChange = eloDeltas[playersData[2].Id];
        }
        if (playerCount > 3) {
            session.Player4Score = playersData[3].Score;
            session.Player4EloChange = eloDeltas[playersData[3].Id];
        }

        foreach (var pd in playersData)
        {
            pd.User.TotalGames++;
            if (session.WinnerId == pd.Id) pd.User.TotalWins++;
            else pd.User.TotalLosses++;
            await _userRepository.UpdateAsync(pd.User);
        }

        await _gameSessionRepository.UpdateAsync(session);

        if (!string.IsNullOrEmpty(session.RoomCode))
        {
            var room = await _roomRepository.GetByCodeAsync(session.RoomCode);
            if (room != null)
            {
                room.Status = RoomStatus.Waiting;
                // do not clear room.Players
                await _roomRepository.UpdateAsync(room);
                await _db.StringSetAsync($"skillduel:room:{room.Code.ToUpper()}:needs_admin", "1", TimeSpan.FromMinutes(10));
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var playerResults = playersData.Select(p => new PlayerGameResult(
            p.Id, p.Username, p.Score, newElos[p.Id], eloDeltas[p.Id]
        )).ToList();

        string? winnerName = session.WinnerId.HasValue ? playersData.First(p => p.Id == session.WinnerId.Value).Username : null;

        await _notificationService.SendGameEndedAsync(sessionId, new GameOverDto(
            session.WinnerId, winnerName, playerResults, session.RoomCode));

        await _db.KeyDeleteAsync(stateKey);
    }

    public async Task<ApiResponse<List<MatchHistoryResponse>>> GetMatchHistoryAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        var sessions = await _gameSessionRepository.GetByUserIdPagedAsync(userId, page, pageSize);
        
        var response = sessions.Select(s => {
            bool isPlayer1 = s.Player1Id == userId;
            var opponent = isPlayer1 ? s.Player2 : s.Player1;
            
            string result = "Draw";
            if (s.WinnerId == userId) result = "Win";
            else if (s.WinnerId != null) result = "Loss";

            return new MatchHistoryResponse
            {
                SessionId = s.Id,
                OpponentUsername = opponent?.Username ?? "Unknown",
                Result = result,
                MyScore = isPlayer1 ? s.Player1Score : s.Player2Score,
                OpponentScore = isPlayer1 ? s.Player2Score : s.Player1Score,
                EloChange = isPlayer1 ? s.Player1EloChange : s.Player2EloChange,
                PlayedAt = s.EndedAt ?? s.StartedAt
            };
        }).ToList();

        return ApiResponse<List<MatchHistoryResponse>>.SuccessResult(response);
    }

    private class OpenTdbResponse { 
        [System.Text.Json.Serialization.JsonPropertyName("response_code")] public int ResponseCode { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("results")] public List<OpenTdbQuestion>? Results { get; set; }
    }

    private class OpenTdbQuestion {
        [System.Text.Json.Serialization.JsonPropertyName("category")] public string? Category { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("type")] public string? Type { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("difficulty")] public string? Difficulty { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("question")] public string? Question { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("correct_answer")] public string? CorrectAnswer { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("incorrect_answers")] public List<string>? IncorrectAnswers { get; set; }
    }

    private int? GetOpenTdbCategoryIdFallback(string categoryName)
    {
        if (categoryName.Contains("General Knowledge")) return 9;
        if (categoryName.Contains("Books")) return 10;
        if (categoryName.Contains("Film")) return 11;
        if (categoryName.Contains("Music") && !categoryName.Contains("Musicals")) return 12;
        if (categoryName.Contains("Musicals") || categoryName.Contains("Theatres")) return 13;
        if (categoryName.Contains("Television")) return 14;
        if (categoryName.Contains("Video Games")) return 15;
        if (categoryName.Contains("Board Games")) return 16;
        if (categoryName.Contains("Science & Nature")) return 17;
        if (categoryName.Contains("Computers")) return 18;
        if (categoryName.Contains("Mathematics")) return 19;
        if (categoryName.Contains("Mythology")) return 20;
        if (categoryName.Contains("Sports")) return 21;
        if (categoryName.Contains("Geography")) return 22;
        if (categoryName.Contains("History")) return 23;
        if (categoryName.Contains("Politics")) return 24;
        if (categoryName.Contains("Art")) return 25;
        if (categoryName.Contains("Celebrities")) return 26;
        if (categoryName.Contains("Animals")) return 27;
        if (categoryName.Contains("Vehicles")) return 28;
        if (categoryName.Contains("Comics")) return 29;
        if (categoryName.Contains("Gadgets")) return 30;
        if (categoryName.Contains("Anime") || categoryName.Contains("Manga")) return 31;
        if (categoryName.Contains("Cartoon") || categoryName.Contains("Animations")) return 32;
        return null;
    }
}
