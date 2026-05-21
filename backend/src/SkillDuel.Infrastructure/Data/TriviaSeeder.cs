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
            if (await _context.Questions.CountAsync() >= 100) return 0;
            
            var totalApprovedCount = _context.Questions.Count(q => q.Status == QuestionStatus.Approved);
            if (totalApprovedCount >= 100)
            {
                _logger.LogInformation("Seeder skipped: Already have {Count} approved questions.", totalApprovedCount);
                return 0;
            }

            var categories = _context.Categories.Where(c => c.OpenTdbId != null).ToList();
            int totalInserted = 0;

            foreach (var category in categories)
            {
                var approvedInCategory = _context.Questions.Count(q => q.CategoryId == category.Id && q.Status == QuestionStatus.Approved);
                if (approvedInCategory >= 40)
                {
                    _logger.LogInformation("Category {Name} already has {Count} approved questions. Skipping.", category.Name, approvedInCategory);
                    continue;
                }

                _logger.LogInformation("Category {Name}: approved count {Count} < 40. Fetching 50 from OpenTDB...", category.Name, approvedInCategory);

                var url = $"https://opentdb.com/api.php?amount=50&category={category.OpenTdbId}&type=multiple";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<TriviaResponse>(response);

                if (result?.Results == null || !result.Results.Any())
                {
                    _logger.LogWarning("Category {Name}: No questions received from Trivia API.", category.Name);
                    await Task.Delay(1500);
                    continue;
                }

                int fetchedCount = result.Results.Count;
                int insertedInCategory = 0;
                int skippedInCategory = 0;

                var random = new Random();
                foreach (var triviaQuestion in result.Results)
                {
                    var questionText = WebUtility.HtmlDecode(triviaQuestion.Question);
                    
                    // Check if question with same text already exists
                    var exists = _context.Questions.Any(q => q.Text == questionText);
                    if (exists)
                    {
                        skippedInCategory++;
                        continue;
                    }

                    var options = triviaQuestion.IncorrectAnswers.Select(a => WebUtility.HtmlDecode(a)).ToList();
                    options = options.OrderBy(x => random.Next()).ToList();
                    
                    var decodedCorrectAnswer = WebUtility.HtmlDecode(triviaQuestion.CorrectAnswer);
                    var correctIndex = random.Next(options.Count + 1);
                    options.Insert(correctIndex, decodedCorrectAnswer);

                    var question = new Question
                    {
                        Text = questionText,
                        Options = options.ToArray(),
                        CorrectOptionIndex = correctIndex,
                        DifficultyLevel = MapDifficulty(WebUtility.HtmlDecode(triviaQuestion.Difficulty)),
                        QuestionType = QuestionType.Multiple,
                        CategoryId = category.Id,
                        Status = QuestionStatus.Approved
                    };

                    _context.Questions.Add(question);
                    insertedInCategory++;
                }

                await _context.SaveChangesAsync();
                totalInserted += insertedInCategory;
                
                _logger.LogInformation("Category {name}: fetched {count}, inserted {inserted}, skipped {skipped}", 
                    category.Name, fetchedCount, insertedInCategory, skippedInCategory);

                // Add delay to avoid rate limiting
                await Task.Delay(1500);
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
