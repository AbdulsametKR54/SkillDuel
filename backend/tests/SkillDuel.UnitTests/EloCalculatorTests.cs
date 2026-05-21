using FluentAssertions;
using SkillDuel.Domain.Utils;
using Xunit;

namespace SkillDuel.UnitTests.Domain;

public class EloCalculatorTests
{
    [Theory]
    [InlineData(1000, 1000, 1, 1016)] // Win against equal: +16
    [InlineData(1000, 1000, 0, 984)]  // Loss against equal: -16
    [InlineData(1000, 1000, 0.5, 1000)] // Draw against equal: 0
    [InlineData(1000, 1500, 1, 1030)] // Win against much stronger: higher gain (~30)
    [InlineData(1500, 1000, 1, 1502)] // Win against much weaker: lower gain (~2)
    public void CalculateNewRating_ShouldReturnExpectedResults(int current, int opponent, double score, int expected)
    {
        // Act
        int result = EloCalculator.CalculateNewRating(current, opponent, score);

        // Assert
        result.Should().BeInRange(expected - 1, expected + 1); // Allow for rounding
    }

    [Fact]
    public void CalculateExpectedScore_ShouldReturnHalf_WhenRatingsAreEqual()
    {
        // Act
        double result = EloCalculator.CalculateExpectedScore(1200, 1200);

        // Assert
        result.Should().Be(0.5);
    }
}
