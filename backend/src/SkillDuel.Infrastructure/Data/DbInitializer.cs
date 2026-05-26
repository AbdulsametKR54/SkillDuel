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

        // 2. Seed all OpenTDB categories (upsert by Slug)
        var allCategories = new[]
        {
            new { Name = "General Knowledge",     Slug = "general-knowledge",    OpenTdbId = 9  },
            new { Name = "Books",                 Slug = "books",                OpenTdbId = 10 },
            new { Name = "Film",                  Slug = "film",                 OpenTdbId = 11 },
            new { Name = "Music",                 Slug = "music",                OpenTdbId = 12 },
            new { Name = "Musicals & Theatres",   Slug = "musicals-theatres",    OpenTdbId = 13 },
            new { Name = "Television",            Slug = "television",           OpenTdbId = 14 },
            new { Name = "Video Games",           Slug = "video-games",          OpenTdbId = 15 },
            new { Name = "Board Games",           Slug = "board-games",          OpenTdbId = 16 },
            new { Name = "Science & Nature",      Slug = "science-nature",       OpenTdbId = 17 },
            new { Name = "Computers",             Slug = "computers",            OpenTdbId = 18 },
            new { Name = "Mathematics",           Slug = "mathematics",          OpenTdbId = 19 },
            new { Name = "Mythology",             Slug = "mythology",            OpenTdbId = 20 },
            new { Name = "Sports",                Slug = "sports",               OpenTdbId = 21 },
            new { Name = "Geography",             Slug = "geography",            OpenTdbId = 22 },
            new { Name = "History",               Slug = "history",              OpenTdbId = 23 },
            new { Name = "Politics",              Slug = "politics",             OpenTdbId = 24 },
            new { Name = "Art",                   Slug = "art",                  OpenTdbId = 25 },
            new { Name = "Celebrities",           Slug = "celebrities",          OpenTdbId = 26 },
            new { Name = "Animals",               Slug = "animals",              OpenTdbId = 27 },
            new { Name = "Vehicles",              Slug = "vehicles",             OpenTdbId = 28 },
            new { Name = "Comics",                Slug = "comics",               OpenTdbId = 29 },
            new { Name = "Gadgets",               Slug = "gadgets",              OpenTdbId = 30 },
            new { Name = "Anime & Manga",         Slug = "anime-manga",          OpenTdbId = 31 },
            new { Name = "Cartoon & Animations",  Slug = "cartoon-animations",   OpenTdbId = 32 },
        };

        foreach (var cat in allCategories)
        {
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Slug == cat.Slug);
            if (existing == null)
            {
                await context.Categories.AddAsync(new Category
                {
                    Name      = cat.Name,
                    Slug      = cat.Slug,
                    OpenTdbId = cat.OpenTdbId
                });
            }
            else if (existing.OpenTdbId == null)
            {
                existing.OpenTdbId = cat.OpenTdbId;
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
