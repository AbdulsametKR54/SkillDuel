using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Friendship;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Application.Services;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConnectionMultiplexer _redis;
    private readonly IGameNotificationService _notificationService;

    public FriendshipService(
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConnectionMultiplexer redis,
        IGameNotificationService notificationService)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _redis = redis;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<object>> SendFriendRequestAsync(Guid senderId, string receiverUsername)
    {
        if (string.IsNullOrWhiteSpace(receiverUsername))
        {
            return ApiResponse<object>.FailureResult("Kullanıcı adı boş olamaz.");
        }

        var sender = await _userRepository.GetByIdAsync(senderId);
        if (sender != null && sender.Username.Equals(receiverUsername, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<object>.FailureResult("Kendinize arkadaşlık isteği gönderemezsiniz.");
        }

        var receiver = await _userRepository.GetByUsernameAsync(receiverUsername);
        if (receiver == null)
        {
            return ApiResponse<object>.FailureResult("İstekte bulunulan kullanıcı bulunamadı.");
        }

        var existing = await _friendshipRepository.GetFriendshipAsync(senderId, receiver.Id);
        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
            {
                return ApiResponse<object>.FailureResult("Bu kullanıcıyla zaten arkadaşsınız.");
            }
            if (existing.Status == FriendshipStatus.Pending)
            {
                if (existing.UserId == senderId)
                {
                    return ApiResponse<object>.FailureResult("Bu kullanıcıya zaten beklemede olan bir isteğiniz var.");
                }
                else
                {
                    existing.Status = FriendshipStatus.Accepted;
                    existing.ActionAt = DateTime.UtcNow;
                    await _friendshipRepository.UpdateAsync(existing);
                    await _unitOfWork.SaveChangesAsync();
                    return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlık isteği otomatik kabul edildi!" });
                }
            }
            if (existing.Status == FriendshipStatus.Blocked)
            {
                return ApiResponse<object>.FailureResult("Arkadaşlık isteği gönderilemiyor.");
            }

            existing.Status = FriendshipStatus.Pending;
            existing.UserId = senderId;
            existing.FriendId = receiver.Id;
            existing.CreatedAt = DateTime.UtcNow;
            existing.ActionAt = null;
            await _friendshipRepository.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            // Emit FriendRequestReceived
            await _notificationService.SendFriendRequestNotificationAsync(receiver.Id, sender.Username);

            return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlık isteği tekrar gönderildi!" });
        }

        var friendship = new Friendship
        {
            UserId = senderId,
            FriendId = receiver.Id,
            Status = FriendshipStatus.Pending
        };

        await _friendshipRepository.AddAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        // Emit FriendRequestReceived
        await _notificationService.SendFriendRequestNotificationAsync(receiver.Id, sender.Username);

        return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlık isteği başarıyla gönderildi!" });
    }

    public async Task<ApiResponse<object>> AcceptFriendRequestAsync(Guid userId, Guid friendshipId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null || friendship.FriendId != userId || friendship.Status != FriendshipStatus.Pending)
        {
            return ApiResponse<object>.FailureResult("Geçersiz arkadaşlık isteği.");
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.ActionAt = DateTime.UtcNow;

        await _friendshipRepository.UpdateAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlık isteği kabul edildi!" });
    }

    public async Task<ApiResponse<object>> DeclineFriendRequestAsync(Guid userId, Guid friendshipId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null || friendship.FriendId != userId || friendship.Status != FriendshipStatus.Pending)
        {
            return ApiResponse<object>.FailureResult("Geçersiz arkadaşlık isteği.");
        }

        friendship.Status = FriendshipStatus.Declined;
        friendship.ActionAt = DateTime.UtcNow;

        await _friendshipRepository.UpdateAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlık isteği reddedildi." });
    }

    public async Task<ApiResponse<object>> RemoveFriendAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendId);
        if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
        {
            return ApiResponse<object>.FailureResult("Arkadaşlık ilişkisi bulunamadı.");
        }

        await _friendshipRepository.DeleteAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.SuccessResult(new { message = "Arkadaşlıktan başarıyla çıkarıldı." });
    }

    public async Task<ApiResponse<List<FriendResponse>>> GetFriendsListAsync(Guid userId)
    {
        var friendships = await _friendshipRepository.GetFriendsByUserIdAsync(userId);
        var db = _redis.GetDatabase();
        
        var list = new List<FriendResponse>();
        foreach (var f in friendships)
        {
            var friendUser = f.UserId == userId ? f.Friend : f.User;
            var presenceKey = $"skillduel:userconnection:{friendUser.Id}";
            bool isOnline = await db.KeyExistsAsync(presenceKey);

            list.Add(new FriendResponse
            {
                FriendshipId = f.Id,
                FriendId = friendUser.Id,
                FriendUsername = friendUser.Username,
                FriendElo = friendUser.EloRating,
                IsOnline = isOnline
            });
        }

        list = list.OrderByDescending(x => x.IsOnline).ThenBy(x => x.FriendUsername).ToList();

        return ApiResponse<List<FriendResponse>>.SuccessResult(list);
    }

    public async Task<ApiResponse<List<FriendRequestResponse>>> GetPendingRequestsAsync(Guid userId)
    {
        var requests = await _friendshipRepository.GetPendingRequestsAsync(userId);
        
        var response = requests.Select(r => new FriendRequestResponse
        {
            FriendshipId = r.Id,
            SenderId = r.UserId,
            SenderUsername = r.User?.Username ?? "Unknown",
            SenderElo = r.User?.EloRating ?? 1000,
            CreatedAt = r.CreatedAt
        }).ToList();

        return ApiResponse<List<FriendRequestResponse>>.SuccessResult(response);
    }
}
