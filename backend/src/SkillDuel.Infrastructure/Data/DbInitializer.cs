using Microsoft.EntityFrameworkCore;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(SkillDuelDbContext context, TriviaSeeder seeder)
    {
        await context.Database.MigrateAsync();

        // 1. Seed Admin User
        if (!await context.Users.AnyAsync(u => u.Email == "admin@skillduel.com"))
        {
            var admin = new User
            {
                Username = "admin",
                Email = "admin@skillduel.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Default password
                Role = "Admin",
                EloRating = 1200
            };
            await context.Users.AddAsync(admin);
        }

        // 2. Seed all OpenTDB categories
        var categories = new[]
        {
            new { Name = "General Knowledge", Slug = "general-knowledge", OpenTdbId = 9 },
            new { Name = "Entertainment: Books", Slug = "entertainment-books", OpenTdbId = 10 },
            new { Name = "Entertainment: Film", Slug = "entertainment-film", OpenTdbId = 11 },
            new { Name = "Entertainment: Music", Slug = "entertainment-music", OpenTdbId = 12 },
            new { Name = "Entertainment: Musicals & Theatres", Slug = "entertainment-musicals-theatres", OpenTdbId = 13 },
            new { Name = "Entertainment: Television", Slug = "entertainment-television", OpenTdbId = 14 },
            new { Name = "Entertainment: Video Games", Slug = "entertainment-video-games", OpenTdbId = 15 },
            new { Name = "Entertainment: Board Games", Slug = "entertainment-board-games", OpenTdbId = 16 },
            new { Name = "Science & Nature", Slug = "science-nature", OpenTdbId = 17 },
            new { Name = "Science: Computers", Slug = "science-computers", OpenTdbId = 18 },
            new { Name = "Science: Mathematics", Slug = "science-mathematics", OpenTdbId = 19 },
            new { Name = "Mythology", Slug = "mythology", OpenTdbId = 20 },
            new { Name = "Sports", Slug = "sports", OpenTdbId = 21 },
            new { Name = "Geography", Slug = "geography", OpenTdbId = 22 },
            new { Name = "History", Slug = "history", OpenTdbId = 23 },
            new { Name = "Politics", Slug = "politics", OpenTdbId = 24 },
            new { Name = "Art", Slug = "art", OpenTdbId = 25 },
            new { Name = "Celebrities", Slug = "celebrities", OpenTdbId = 26 },
            new { Name = "Animals", Slug = "animals", OpenTdbId = 27 },
            new { Name = "Vehicles", Slug = "vehicles", OpenTdbId = 28 },
            new { Name = "Entertainment: Comics", Slug = "entertainment-comics", OpenTdbId = 29 },
            new { Name = "Science: Gadgets", Slug = "science-gadgets", OpenTdbId = 30 },
            new { Name = "Entertainment: Japanese Anime & Manga", Slug = "entertainment-anime-manga", OpenTdbId = 31 },
            new { Name = "Entertainment: Cartoon & Animations", Slug = "entertainment-cartoon-animations", OpenTdbId = 32 },
        };

        // Delete old wrong categories that don't have a valid OpenTdbId
        var validOpenTdbIds = categories.Select(c => (int?)c.OpenTdbId).ToHashSet();
        var oldCategories = await context.Categories
            .Where(c => c.OpenTdbId == null || !validOpenTdbIds.Contains(c.OpenTdbId))
            .ToListAsync();

        if (oldCategories.Any())
        {
            context.Categories.RemoveRange(oldCategories);
        }

        // Insert new ones or skip if existing by OpenTdbId
        var existingOpenTdbIds = (await context.Categories
            .Where(c => c.OpenTdbId != null)
            .Select(c => c.OpenTdbId.Value)
            .ToListAsync())
            .ToHashSet();

        foreach (var cat in categories)
        {
            if (!existingOpenTdbIds.Contains(cat.OpenTdbId))
            {
                await context.Categories.AddAsync(new Category
                {
                    Name = cat.Name,
                    Slug = cat.Slug,
                    OpenTdbId = cat.OpenTdbId
                });
            }
        }
        await context.SaveChangesAsync();

        // 3. Seed questions ONLY if we have 0 approved questions
        int approvedCount = await context.Questions.CountAsync(q => q.Status == QuestionStatus.Approved);
        
        if (approvedCount < 100)
        {
            // Seed questions from OpenTDB
            await seeder.SeedQuestionsAsync();
        }
    }
}
