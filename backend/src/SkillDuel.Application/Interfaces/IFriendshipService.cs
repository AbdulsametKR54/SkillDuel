using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Friendship;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IFriendshipService
{
    Task<ApiResponse<object>> SendFriendRequestAsync(Guid senderId, string receiverUsername);
    Task<ApiResponse<object>> AcceptFriendRequestAsync(Guid userId, Guid friendshipId);
    Task<ApiResponse<object>> DeclineFriendRequestAsync(Guid userId, Guid friendshipId);
    Task<ApiResponse<object>> RemoveFriendAsync(Guid userId, Guid friendId);
    Task<ApiResponse<List<FriendResponse>>> GetFriendsListAsync(Guid userId);
    Task<ApiResponse<List<FriendRequestResponse>>> GetPendingRequestsAsync(Guid userId);
}
