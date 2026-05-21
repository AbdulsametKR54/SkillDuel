using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.User;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserResponse>> UpdateUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<LeaderboardResponse>>> GetLeaderboardAsync(int limit = 50);
    Task<ApiResponse<List<UserCategoryStatResponse>>> GetUserCategoryStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}
