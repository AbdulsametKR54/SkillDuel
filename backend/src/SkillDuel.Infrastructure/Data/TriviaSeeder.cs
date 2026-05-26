using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkillDuel.Infrastructure.Data;

public class TriviaSeeder
{
    private readonly SkillDuelDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TriviaSeeder> _logger;

    public TriviaSeeder(SkillDuelDbContext context, HttpClient httpClient, ILogger<TriviaSeeder> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> SeedQuestionsAsync()
    {
        try
        {
            // Early exit: enough approved questions already exist
            if (await _context.Questions.CountAsync(q => q.Status == QuestionStatus.Approved) >= 100)
            {
                _logger.LogInformation("Seeder skipped: Already have 100+ approved questions.");
                return 0;
            }

            // --- 1 DB query: load ALL existing question texts into a HashSet ---
            var existingTexts = (await _context.Questions
                .Select(q => q.Text)
                .ToListAsync())
                .ToHashSet();

            _logger.LogInformation("Loaded {Count} existing question texts into dedup cache.", existingTexts.Count);

            var categories = await _context.Categories
                .Where(c => c.OpenTdbId != null)
                .ToListAsync();

            int totalInserted = 0;
            var random = new Random();

            foreach (var category in categories)
            {
                var approvedInCategory = await _context.Questions
                    .CountAsync(q => q.CategoryId == category.Id && q.Status == QuestionStatus.Approved);

                if (approvedInCategory >= 40)
                {
                    _logger.LogInformation("Category {Name}: already has {Count} approved questions. Skipping.",
                        category.Name, approvedInCategory);
                    continue;
                }

                _logger.LogInformation("Category {Name}: approved={Count}. Fetching from OpenTDB...",
                    category.Name, approvedInCategory);

                var url = $"https://opentdb.com/api.php?amount=50&category={category.OpenTdbId}&type=multiple";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<TriviaResponse>(response);

                if (result?.Results == null || !result.Results.Any())
                {
                    _logger.LogWarning("Category {Name}: No questions received from OpenTDB.", category.Name);
                    await Task.Delay(1500);
                    continue;
                }

                // --- Decode all fetched questions in memory ---
                var decoded = result.Results.Select(t => new
                {
                    Text       = WebUtility.HtmlDecode(t.Question),
                    Correct    = WebUtility.HtmlDecode(t.CorrectAnswer),
                    Incorrect  = t.IncorrectAnswers.Select(a => WebUtility.HtmlDecode(a)).ToList(),
                    Difficulty = WebUtility.HtmlDecode(t.Difficulty)
                }).ToList();

                // --- In-memory dedup against the HashSet (no extra DB queries) ---
                var newDecoded = decoded.Where(d => !existingTexts.Contains(d.Text)).ToList();

                if (!newDecoded.Any())
                {
                    _logger.LogInformation("Category {Name}: all {Count} fetched questions already exist. Skipping.",
                        category.Name, decoded.Count);
                    await Task.Delay(1500);
                    continue;
                }

                // --- Build Question entities ---
                var newQuestions = newDecoded.Select(d =>
                {
                    var options = d.Incorrect.OrderBy(_ => random.Next()).ToList();
                    var correctIndex = random.Next(options.Count + 1);
                    options.Insert(correctIndex, d.Correct);

                    return new Question
                    {
                        Text               = d.Text,
                        Options            = options.ToArray(),
                        CorrectOptionIndex = correctIndex,
                        DifficultyLevel    = MapDifficulty(d.Difficulty),
                        QuestionType       = QuestionType.Multiple,
                        CategoryId         = category.Id,
                        Status             = QuestionStatus.Approved
                    };
                }).ToList();

                // --- 1 bulk INSERT per category ---
                await _context.Questions.AddRangeAsync(newQuestions);
                await _context.SaveChangesAsync();

                // Keep HashSet current so cross-category duplicates are also caught
                foreach (var q in newQuestions) existingTexts.Add(q.Text);

                totalInserted += newQuestions.Count;

                _logger.LogInformation("Category {Name}: fetched={Fetched}, new={New}, skipped={Skipped}",
                    category.Name, decoded.Count, newQuestions.Count, decoded.Count - newQuestions.Count);

                await Task.Delay(1500); // Respect OpenTDB rate limit
            }

            _logger.LogInformation("Seeding complete. Total inserted: {Total}", totalInserted);
            return totalInserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding trivia: {Message}", ex.Message);
            return 0;
        }
    }


    private DifficultyLevel MapDifficulty(string difficulty)
    {
        return difficulty.ToLower() switch
        {
            "easy" => DifficultyLevel.Easy,
            "medium" => DifficultyLevel.Medium,
            "hard" => DifficultyLevel.Hard,
            _ => DifficultyLevel.Medium
        };
    }

    private int GetOpenTdbCategoryId(string categoryName)
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
        return 9; // Fallback to General Knowledge
    }
}

public class TriviaResponse { 
    [JsonPropertyName("response_code")] public int ResponseCode { get; set; }
    [JsonPropertyName("results")] public List<TriviaQuestion> Results { get; set; }
}

public class TriviaQuestion {
    [JsonPropertyName("category")] public string Category { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("difficulty")] public string Difficulty { get; set; }
    [JsonPropertyName("question")] public string Question { get; set; }
    [JsonPropertyName("correct_answer")] public string CorrectAnswer { get; set; }
    [JsonPropertyName("incorrect_answers")] public List<string> IncorrectAnswers { get; set; }
}
