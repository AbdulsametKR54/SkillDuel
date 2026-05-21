using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.User;
using SkillDuel.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserCategoryStatRepository _userCategoryStatRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository, 
        IUserCategoryStatRepository userCategoryStatRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userCategoryStatRepository = userCategoryStatRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserResponse>.FailureResult("User not found.");
        }

        var response = new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            EloRating = user.EloRating,
            TotalWins = user.TotalWins,
            TotalLosses = user.TotalLosses,
            TotalGames = user.TotalGames,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserResponse>.SuccessResult(response);
    }

    public async Task<ApiResponse<UserResponse>> UpdateUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default)
    {
        // Validate format: 3-20 chars, alphanumeric + underscore only
        if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3 || newUsername.Length > 20)
        {
            return ApiResponse<UserResponse>.FailureResult("Username must be between 3 and 20 characters.");
        }

        if (!Regex.IsMatch(newUsername, @"^[a-zA-Z0-9_]+$"))
        {
            return ApiResponse<UserResponse>.FailureResult("Username may only contain letters, numbers, and underscores.");
        }

        // Unique check
        var existing = await _userRepository.GetByUsernameAsync(newUsername, cancellationToken);
        if (existing != null && existing.Id != userId)
        {
            return ApiResponse<UserResponse>.FailureResult("That username is already taken.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserResponse>.FailureResult("User not found.");
        }

        user.Username = newUsername;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            EloRating = user.EloRating,
            TotalWins = user.TotalWins,
            TotalLosses = user.TotalLosses,
            TotalGames = user.TotalGames,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserResponse>.SuccessResult(response);
    }

    public async Task<ApiResponse<List<LeaderboardResponse>>> GetLeaderboardAsync(int limit = 50)
    {
        var users = await _userRepository.GetTopByEloAsync(limit);

        var response = users.Select((user, index) => new LeaderboardResponse
        {
            Rank = index + 1,
            UserId = user.Id,
            Username = user.Username,
            EloRating = user.EloRating,
            TotalWins = user.TotalWins,
            TotalLosses = user.TotalLosses
        }).ToList();

        return ApiResponse<List<LeaderboardResponse>>.SuccessResult(response);
    }

    public async Task<ApiResponse<List<UserCategoryStatResponse>>> GetUserCategoryStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var stats = await _userCategoryStatRepository.GetByUserIdAsync(userId);
        
        var response = stats.Select(s => new UserCategoryStatResponse
        {
            CategoryName = s.Category?.Name ?? "General",
            CategorySlug = s.Category?.Slug ?? "general",
            CorrectAnswersCount = s.CorrectAnswersCount,
            TotalAnswersCount = s.TotalAnswersCount
        }).ToList();

        return ApiResponse<List<UserCategoryStatResponse>>.SuccessResult(response);
    }
}
