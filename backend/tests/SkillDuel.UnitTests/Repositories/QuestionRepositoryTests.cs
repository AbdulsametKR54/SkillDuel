using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Infrastructure.Data;
using SkillDuel.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SkillDuel.UnitTests.Repositories;

public class QuestionRepositoryTests
{
    private SkillDuelDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<SkillDuelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SkillDuelDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectItems_WhenFilteredByCategory()
    {
        // Arrange
        var context = GetContext();
        var category1 = new Category { Id = Guid.NewGuid(), Name = "Cat1", Slug = "cat1" };
        var category2 = new Category { Id = Guid.NewGuid(), Name = "Cat2", Slug = "cat2" };
        
        var questions = new List<Question>
        {
            new() { Text = "Q1", CategoryId = category1.Id, Options = new[] { "A" }, CorrectOptionIndex = 0 },
            new() { Text = "Q2", CategoryId = category1.Id, Options = new[] { "A" }, CorrectOptionIndex = 0 },
            new() { Text = "Q3", CategoryId = category2.Id, Options = new[] { "A" }, CorrectOptionIndex = 0 }
        };

        await context.Categories.AddRangeAsync(category1, category2);
        await context.Questions.AddRangeAsync(questions);
        await context.SaveChangesAsync();

        var repository = new QuestionRepository(context);

        // Act
        var (items, total) = await repository.GetPagedAsync(category1.Id, null, 1, 10);

        // Assert
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.All(x => x.CategoryId == category1.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldApplyPagination_Correctly()
    {
        // Arrange
        var context = GetContext();
        var category = new Category { Id = Guid.NewGuid(), Name = "Cat", Slug = "cat" };
        await context.Categories.AddAsync(category);
        
        for (int i = 1; i <= 15; i++)
        {
            context.Questions.Add(new Question 
            { 
                Text = $"Q{i}", 
                CategoryId = category.Id, 
                Options = new[] { "A" }, 
                CorrectOptionIndex = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        var repository = new QuestionRepository(context);

        // Act
        var resultPage1 = await repository.GetPagedAsync(null, null, 1, 10);
        var resultPage2 = await repository.GetPagedAsync(null, null, 2, 10);

        // Assert
        resultPage1.TotalCount.Should().Be(15);
        resultPage1.Items.Should().HaveCount(10);
        resultPage2.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByDifficulty()
    {
        // Arrange
        var context = GetContext();
        var category = new Category { Id = Guid.NewGuid(), Name = "Cat", Slug = "cat" };
        await context.Categories.AddAsync(category);
        
        context.Questions.Add(new Question 
        { 
            Text = "Easy Q", 
            CategoryId = category.Id, 
            DifficultyLevel = DifficultyLevel.Easy,
            Options = new[] { "A" }, 
            CorrectOptionIndex = 0,
            CreatedAt = DateTime.UtcNow
        });
        context.Questions.Add(new Question 
        { 
            Text = "Hard Q", 
            CategoryId = category.Id, 
            DifficultyLevel = DifficultyLevel.Hard,
            Options = new[] { "A" }, 
            CorrectOptionIndex = 0,
            CreatedAt = DateTime.UtcNow.AddSeconds(1)
        });
        await context.SaveChangesAsync();

        var repository = new QuestionRepository(context);

        // Act
        var (items, total) = await repository.GetPagedAsync(null, DifficultyLevel.Hard, 1, 10);

        // Assert
        total.Should().Be(1);
        items.Should().NotBeEmpty();
        items.First().Text.Should().Be("Hard Q");
    }
}
