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

        // 2. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Geography", Slug = "geography" },
                new() { Name = "Science", Slug = "science" },
                new() { Name = "History", Slug = "history" }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 3. Seed questions ONLY if we have 0 approved questions
        int approvedCount = await context.Questions.CountAsync(q => q.Status == QuestionStatus.Approved);
        
        if (approvedCount < 100)
        {
            // Seed questions from OpenTDB
            await seeder.SeedQuestionsAsync();
        }
    }
}
