using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkillDuel.Api.Hubs;
using SkillDuel.Application.Common;

using SkillDuel.Application.DTOs.Room;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using System.Security.Claims;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IHubContext<GameHub, IGameHub> _hubContext;
    private readonly SkillDuel.Infrastructure.Data.SkillDuelDbContext _dbContext;
    private readonly StackExchange.Redis.IConnectionMultiplexer _redis;

    public RoomsController(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ICategoryRepository categoryRepository,
        IHubContext<GameHub, IGameHub> hubContext,
        SkillDuel.Infrastructure.Data.SkillDuelDbContext dbContext,
        StackExchange.Redis.IConnectionMultiplexer redis)
    {
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _categoryRepository = categoryRepository;
        _hubContext = hubContext;
        _dbContext = dbContext;
        _redis = redis;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = GetUserId();
        
        string code;
        bool isUnique = false;
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        do
        {
            code = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            var existing = await _roomRepository.GetByCodeAsync(code);
            if (existing == null) isUnique = true;
        } while (!isUnique);

        var room = new Room
        {
            Code = code,
            Name = request.Name,
            HostId = userId,
            IsPrivate = request.IsPrivate,
            Password = string.IsNullOrEmpty(request.Password) ? null : BCrypt.Net.BCrypt.HashPassword(request.Password),
            CategoryId = request.CategoryId,
            Difficulty = request.Difficulty,
            QuestionType = request.QuestionType,
            RoundCount = request.RoundCount,
            MaxPlayers = request.MaxPlayers,
            Status = RoomStatus.Waiting
        };

        var hostPlayer = new RoomPlayer
        {
            RoomId = room.Id,
            UserId = userId,
            SlotNumber = 1
        };

        room.Players.Add(hostPlayer);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        Console.WriteLine($"[CreateRoom] Room created: {room.Code}, IsPrivate: {room.IsPrivate}");

        // Load navigation properties for response
        room.Host = (await _userRepository.GetByIdAsync(userId))!;

        return Ok(ApiResponse<RoomResponse>.SuccessResult(MapToResponse(room)));
    }


    [HttpGet]
    public async Task<IActionResult> GetRooms([FromQuery] RoomSearchRequest request)
    {
        var (items, totalCount) = await _roomRepository.GetPublicWaitingRoomsAsync(
            page: request.Page,
            pageSize: request.PageSize,
            searchName: request.SearchName,
            categoryId: request.CategoryId,
            roundCount: request.RoundCount
        );
        
        var responseItems = items.Select(r => MapToResponse(r)).ToList();
        
        var response = new PaginatedRoomResponse
        {
            TotalCount = totalCount,
            Items = responseItems
        };
        
        return Ok(ApiResponse<PaginatedRoomResponse>.SuccessResult(response));
    }


    [HttpGet("{code}")]
    public async Task<IActionResult> GetRoomByCode(string code)
    {
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        if (room == null) return NotFound(ApiResponse<RoomResponse>.FailureResult("Room not found."));
        
        return Ok(ApiResponse<RoomResponse>.SuccessResult(MapToResponse(room)));
    }


    [HttpPost("{code}/join")]
    public async Task<IActionResult> JoinRoom(string code, [FromBody] JoinRoomRequest? request)
    {
        Console.WriteLine($"[JoinRoom] Attempting to join room: {code}");
        var userId = GetUserId();
        Console.WriteLine($"[JoinRoom] User ID: {userId}");
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        
        if (room == null) 
        {
            Console.WriteLine($"[JoinRoom] Room not found for code: {code}");
            return NotFound(ApiResponse<RoomResponse>.FailureResult("Room not found."));
        }
        
        Console.WriteLine($"[JoinRoom] Room found. Status: {room.Status}, HostId: {room.HostId}, GuestId: {room.GuestId}, IsPrivate: {room.IsPrivate}");
        
        if (room.Status != RoomStatus.Waiting) 
        {
            Console.WriteLine($"[JoinRoom] Room is not waiting. Actual status: {room.Status}");
            return BadRequest(ApiResponse<RoomResponse>.FailureResult($"Room is not joinable. Status: {room.Status}"));
        }
        
        if (room.Players.Any(p => p.UserId == userId)) 
        {
            Console.WriteLine($"[JoinRoom] User is already in room. Returning existing state.");
            return Ok(ApiResponse<RoomResponse>.SuccessResult(MapToResponse(room)));
        }
        
        if (room.Players.Count >= room.MaxPlayers) 
        {
            Console.WriteLine($"[JoinRoom] Room is already full. Count: {room.Players.Count}");
            return BadRequest(ApiResponse<RoomResponse>.FailureResult("Room is full."));
        }

        if (room.IsPrivate)
        {
            var db = _redis.GetDatabase();
            var isInvited = await db.KeyExistsAsync($"skillduel:invite:{room.Code.ToUpper()}:{userId}");
            
            if (!isInvited)
            {
                if (request == null || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(room.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, room.Password))
                {
                    Console.WriteLine($"[JoinRoom] Invalid password provided.");
                    return BadRequest(ApiResponse<RoomResponse>.FailureResult("Invalid password."));
                }
            }
            else
            {
                // Consume the invite
                await db.KeyDeleteAsync($"skillduel:invite:{room.Code.ToUpper()}:{userId}");
            }
        }

        Console.WriteLine($"[JoinRoom] All checks passed. Updating room for player {userId}");
        
        var occupiedSlots = room.Players.Select(p => p.SlotNumber).ToHashSet();
        int nextSlot = 1;
        for (int i = 1; i <= room.MaxPlayers; i++)
        {
            if (!occupiedSlots.Contains(i))
            {
                nextSlot = i;
                break;
            }
        }

        var newPlayer = new RoomPlayer
        {
            RoomId = room.Id,
            UserId = userId,
            SlotNumber = nextSlot
        };
        room.Players.Add(newPlayer);
        
        // For backwards compatibility and 2-player rooms, set GuestId if it's the second player
        if (room.Players.Count == 2 && room.GuestId == null)
        {
            room.GuestId = userId;
        }

        if (room.Players.Count == room.MaxPlayers)
        {
            room.Status = RoomStatus.Ready;
        }
        await _unitOfWork.SaveChangesAsync();

        // Notify host via SignalR
        var guest = await _userRepository.GetByIdAsync(userId);
        await _hubContext.Clients.Group(room.Code).GuestJoined(guest?.Username ?? "Guest");

        return Ok(ApiResponse<RoomResponse>.SuccessResult(MapToResponse(room)));
    }


    [HttpDelete("{code}")]
    public async Task<IActionResult> CloseRoom(string code)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        
        if (room == null) return NotFound(ApiResponse.FailureResult("Room not found."));
        if (room.HostId != userId) return Forbid();

        room.Status = RoomStatus.Closed;
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.Group(room.Code.ToUpper()).RoomClosed();

        return Ok(ApiResponse.SuccessResult());
    }

    [HttpPost("{code}/leave")]
    public async Task<IActionResult> LeaveRoom(string code)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        
        if (room == null) return NotFound(ApiResponse.FailureResult("Room not found."));

        var player = room.Players.FirstOrDefault(p => p.UserId == userId);
        if (player != null)
        {
            room.Players.Remove(player);
            _dbContext.RoomPlayers.Remove(player);
            
            if (room.GuestId == userId)
            {
                room.GuestId = null;
            }

            if (room.Players.Count < room.MaxPlayers && room.Status == RoomStatus.Ready)
            {
                room.Status = RoomStatus.Waiting;
            }

            await _unitOfWork.SaveChangesAsync();
            await _hubContext.Clients.Group(room.Code.ToUpper()).PlayerLeft(new { userId = userId });
        }

        return Ok(ApiResponse.SuccessResult());
    }

    [HttpPost("{code}/kick/{targetUserId}")]
    public async Task<IActionResult> KickUser(string code, Guid targetUserId)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        if (room == null) return NotFound(ApiResponse.FailureResult("Room not found."));

        var db = _redis.GetDatabase();
        var adminStr = await db.StringGetAsync($"skillduel:room:{room.Code.ToUpper()}:admin");
        var currentAdminId = adminStr.HasValue ? Guid.Parse(adminStr.ToString()) : room.HostId;

        if (currentAdminId != userId) return Forbid();

        var player = room.Players.FirstOrDefault(p => p.UserId == targetUserId);
        if (player == null) return NotFound(ApiResponse.FailureResult("Player not found in room."));

        room.Players.Remove(player);
        _dbContext.RoomPlayers.Remove(player);
        
        if (room.GuestId == targetUserId)
        {
            room.GuestId = null;
        }

        if (room.Players.Count < room.MaxPlayers && room.Status == RoomStatus.Ready)
        {
            room.Status = RoomStatus.Waiting;
        }

        // Seçenek B: Geçici admin ana kurucuyu (host) atarsa, mülkiyet admine kalıcı olarak geçer.
        if (targetUserId == room.HostId)
        {
            room.HostId = currentAdminId;
            await db.KeyDeleteAsync($"skillduel:room:{room.Code.ToUpper()}:admin");
            // Notify clients of the permanent host change so UI re-evaluates
            await _hubContext.Clients.Group(room.Code.ToUpper()).HostChanged(currentAdminId);
        }

        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.User(targetUserId.ToString()).KickedFromRoom();
        await _hubContext.Clients.Group(room.Code.ToUpper()).PlayerLeft(new { userId = targetUserId });

        return Ok(ApiResponse.SuccessResult());
    }

    [HttpPut("{code}/settings")]
    public async Task<IActionResult> UpdateSettings(string code, [FromBody] UpdateRoomSettingsRequest request)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        if (room == null) return NotFound(ApiResponse.FailureResult("Room not found."));

        var db = _redis.GetDatabase();
        var adminStr = await db.StringGetAsync($"skillduel:room:{room.Code.ToUpper()}:admin");
        var currentAdminId = adminStr.HasValue ? Guid.Parse(adminStr.ToString()) : room.HostId;

        if (currentAdminId != userId) return Forbid();

        room.CategoryId = request.CategoryId;
        room.Difficulty = request.Difficulty;
        room.QuestionType = request.QuestionType;
        room.RoundCount = request.RoundCount;

        await _unitOfWork.SaveChangesAsync();

        var settings = new
        {
            categoryId = room.CategoryId,
            difficulty = room.Difficulty,
            questionType = room.QuestionType,
            roundCount = room.RoundCount
        };

        await _hubContext.Clients.Group(room.Code.ToUpper()).RoomSettingsUpdated(settings);

        return Ok(ApiResponse.SuccessResult());
    }

    [HttpPost("{code}/delegate-admin/{targetUserId}")]
    public async Task<IActionResult> DelegateAdmin(string code, Guid targetUserId)
    {
        var userId = GetUserId();
        var room = await _roomRepository.GetByCodeAsync(code.ToUpper());
        if (room == null) return NotFound(ApiResponse.FailureResult("Room not found."));

        var db = _redis.GetDatabase();
        var adminKey = $"skillduel:room:{room.Code.ToUpper()}:admin";
        var adminStr = await db.StringGetAsync(adminKey);
        var currentAdminId = adminStr.HasValue ? Guid.Parse(adminStr.ToString()) : room.HostId;

        if (currentAdminId != userId) return Forbid();

        var player = room.Players.FirstOrDefault(p => p.UserId == targetUserId);
        if (player == null) return NotFound(ApiResponse.FailureResult("Target user not in room."));

        await db.StringSetAsync(adminKey, targetUserId.ToString(), TimeSpan.FromHours(1));

        await _hubContext.Clients.Group(room.Code.ToUpper()).HostChanged(targetUserId);

        return Ok(ApiResponse.SuccessResult());
    }


    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!.Value);
    }

    private RoomResponse MapToResponse(Room room)
    {
        return new RoomResponse
        {
            Id = room.Id,
            Code = room.Code,
            Name = room.Name,
            HostId = room.HostId,
            HostUsername = room.Host?.Username ?? "Unknown",
            GuestId = room.GuestId,
            GuestUsername = room.Guest?.Username,
            IsPrivate = room.IsPrivate,
            CategoryId = room.CategoryId,
            CategoryName = room.Category?.Name,
            Difficulty = room.Difficulty,
            QuestionType = room.QuestionType,
            RoundCount = room.RoundCount,
            MaxPlayers = room.MaxPlayers,
            Status = room.Status.ToString(),
            CreatedAt = room.CreatedAt,
            Players = room.Players.DistinctBy(p => p.UserId).Select(p => new RoomPlayerResponse
            {
                UserId = p.UserId,
                Username = p.User?.Username ?? (p.UserId == room.HostId ? room.Host?.Username : room.Guest?.Username) ?? "Unknown",
                SlotNumber = p.SlotNumber
            }).OrderBy(p => p.SlotNumber).ToList(),
            CurrentAdminId = _redis.GetDatabase().StringGet($"skillduel:room:{room.Code.ToUpper()}:admin").HasValue 
                ? Guid.Parse(_redis.GetDatabase().StringGet($"skillduel:room:{room.Code.ToUpper()}:admin").ToString()) 
                : room.HostId
        };
    }
}
