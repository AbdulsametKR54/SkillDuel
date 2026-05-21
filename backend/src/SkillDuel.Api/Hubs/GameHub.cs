using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace SkillDuel.Api.Hubs;

[Authorize]
public class GameHub : Hub<IGameHub>
{
    private readonly IMatchmakingService _matchmakingService;
    private readonly IGameService _gameService;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConnectionMultiplexer _redis;

    public GameHub(
        IMatchmakingService matchmakingService, 
        IGameService gameService,
        IGameSessionRepository gameSessionRepository,
        IRoomRepository roomRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConnectionMultiplexer redis)
    {
        _matchmakingService = matchmakingService;
        _gameService = gameService;
        _gameSessionRepository = gameSessionRepository;
        _roomRepository = roomRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _redis = redis;
    }

    public async Task JoinRoomGroup(string roomCode)
    {
        var userId = GetUserId();
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"skillduel:userconnection:{userId}", Context.ConnectionId, TimeSpan.FromHours(1));

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode.ToUpper());
    }

    public async Task LeaveRoomGroup(string roomCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode.ToUpper());
    }

    public async Task SendRoomMessage(string roomCode, string message)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        var username = user?.Username ?? "Unknown";
        var timestamp = DateTime.UtcNow.ToString("HH:mm");
        
        await Clients.Group(roomCode.ToUpper()).RoomMessage(username, message, timestamp);
    }

    public async Task StartRoomGame(string roomCode)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(roomCode.ToUpper());

        if (room == null || room.HostId != userId)
        {
            throw new HubException("Only host can start the game.");
        }

        if (room.Players.Count < 2)
        {
            throw new HubException("Waiting for opponents to join.");
        }

        if (room.Status == RoomStatus.InGame)
        {
            return; // Already started
        }

        var db = _redis.GetDatabase();
        string lockKey = $"room:starting:{roomCode.ToUpper()}";
        if (!await db.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(10), When.NotExists))
        {
            return; // Another request is already starting the room
        }

        var p1 = room.Players.OrderBy(p => p.SlotNumber).ElementAtOrDefault(0);
        var p2 = room.Players.OrderBy(p => p.SlotNumber).ElementAtOrDefault(1);
        var p3 = room.Players.OrderBy(p => p.SlotNumber).ElementAtOrDefault(2);
        var p4 = room.Players.OrderBy(p => p.SlotNumber).ElementAtOrDefault(3);

        // Create Game Session
        var session = new GameSession
        {
            Player1Id = p1!.UserId,
            Player2Id = p2!.UserId,
            Player3Id = p3?.UserId,
            Player4Id = p4?.UserId,
            Status = GameStatus.Active
        };

        await _gameSessionRepository.AddAsync(session);
        room.Status = RoomStatus.InGame;
        await _unitOfWork.SaveChangesAsync();

        // Explicitly add all players to the new session group
        foreach (var player in room.Players)
        {
            var conn = await db.StringGetAsync($"skillduel:userconnection:{player.UserId}");
            if (!conn.IsNull) await Groups.AddToGroupAsync(conn.ToString(), session.Id.ToString());
        }

        // Map difficulty and type
        DifficultyLevel? diff = null;
        if (!string.IsNullOrEmpty(room.Difficulty) && Enum.TryParse<DifficultyLevel>(room.Difficulty, out var d)) diff = d;
        
        QuestionType? type = null;
        if (!string.IsNullOrEmpty(room.QuestionType) && Enum.TryParse<QuestionType>(room.QuestionType, out var t)) type = t;

        GameMode mode = room.RoundCount <= 5 ? GameMode.Short : GameMode.Long;

        // Fetch players for info
        var player1 = await _userRepository.GetByIdAsync(p1.UserId);
        var player2 = await _userRepository.GetByIdAsync(p2.UserId);

        // Notify Room (Optional, but we keep it for backward compatibility or simple UI updates)
        await Clients.Group(roomCode.ToUpper()).RoomGameStarting(session.Id);

        var allPlayersData = room.Players.Select(p => new {
            Id = p.UserId,
            Username = p.User!.Username,
            Elo = p.User!.EloRating
        }).ToList();

        // Send MatchFound so frontend useGameStore initializes correctly
        foreach (var player in room.Players)
        {
            await Clients.User(player.UserId.ToString()).MatchFound(new
            {
                SessionId = session.Id,
                MyId = player.UserId,
                Players = allPlayersData
            });
        }

        // Delay starting the game to give clients time to navigate to the duel page and connect
        await Task.Delay(4000);

        // Start Game logic
        await _gameService.StartGameAsync(session.Id, mode,
            room.CategoryId, room.CategoryId,
            diff, diff,
            type, type,
            p1!.UserId, p2!.UserId,
            p1.User!.Username, p2.User!.Username,
            p3?.UserId, p4?.UserId,
            p3?.User?.Username, p4?.User?.Username);
    }


    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"skillduel:userconnection:{userId}", Context.ConnectionId, TimeSpan.FromHours(24));

        var activeSession = await _gameSessionRepository.GetActiveSessionByUserIdAsync(userId);
        
        if (activeSession != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, activeSession.Id.ToString());
            Console.WriteLine($"[SignalR] User {userId} reconnected to session group {activeSession.Id}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinMatchmaking(GameMode mode, Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType)
    {
        var userId = GetUserId();
        await _matchmakingService.JoinQueueAsync(userId, mode, categoryId, difficulty, questionType);
    }

    public async Task LeaveMatchmaking()
    {
        var userId = GetUserId();
        await _matchmakingService.LeaveQueueAsync(userId);
    }

    public async Task SubmitAnswer(Guid sessionId, int optionIndex, long timeMs)
    {
        var userId = GetUserId();
        await _gameService.SubmitAnswerAsync(sessionId, userId, optionIndex, timeMs);
    }

    public async Task SendEmote(Guid sessionId, string emote)
    {
        var userId = GetUserId();
        await Clients.Group(sessionId.ToString()).EmoteReceived(userId, emote);
    }

    public async Task InviteFriend(Guid friendId, string roomCode)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        var senderUsername = user?.Username ?? "Bir oyuncu";
        
        await Clients.User(friendId.ToString()).FriendInviteReceived(new
        {
            senderId = userId,
            senderUsername = senderUsername,
            roomCode = roomCode
        });
    }

    public async Task JoinGameGroup(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"skillduel:userconnection:{userId}");
        await _matchmakingService.LeaveQueueAsync(userId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new HubException("User identity not found.");
        }
        return userId;
    }
}
