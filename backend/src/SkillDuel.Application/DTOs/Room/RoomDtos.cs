using System;

namespace SkillDuel.Application.DTOs.Room;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public string? Password { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Difficulty { get; set; }
    public string? QuestionType { get; set; }
    public int RoundCount { get; set; } = 5;
    public int MaxPlayers { get; set; } = 2;
}

public class RoomResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid HostId { get; set; }
    public string HostUsername { get; set; } = string.Empty;
    public Guid? GuestId { get; set; }
    public string? GuestUsername { get; set; }
    public bool IsPrivate { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Difficulty { get; set; }
    public string? QuestionType { get; set; }
    public int RoundCount { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<RoomPlayerResponse> Players { get; set; } = new();
}

public class RoomPlayerResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
}

public class JoinRoomRequest
{
    public string? Password { get; set; }
}
