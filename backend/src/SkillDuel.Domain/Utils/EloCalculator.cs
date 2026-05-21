using System;

namespace SkillDuel.Domain.Utils;

public static class EloCalculator
{
    public static int CalculateNewRating(int currentRating, int opponentRating, double actualScore, int kFactor = 32)
    {
        double expectedScore = CalculateExpectedScore(currentRating, opponentRating);
        int newRating = (int)Math.Round(currentRating + kFactor * (actualScore - expectedScore));
        return newRating;
    }

    public static double CalculateExpectedScore(int playerRating, int opponentRating)
    {
        return 1.0 / (1.0 + Math.Pow(10, (double)(opponentRating - playerRating) / 400));
    }
}
