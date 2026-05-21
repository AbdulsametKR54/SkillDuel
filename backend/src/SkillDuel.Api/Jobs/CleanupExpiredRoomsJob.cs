using Microsoft.EntityFrameworkCore;
using SkillDuel.Domain.Enums;
using SkillDuel.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Api.Jobs;

public class CleanupExpiredRoomsJob
{
    private readonly SkillDuelDbContext _context;

    public CleanupExpiredRoomsJob(SkillDuelDbContext context)
    {
        _context = context;
    }

    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var expiredRooms = await _context.Rooms
            .Where(r => r.Status == RoomStatus.Waiting && r.ExpiresAt <= now)
            .ToListAsync();

        foreach (var room in expiredRooms)
        {
            room.Status = RoomStatus.Closed;
        }

        if (expiredRooms.Any())
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"[CleanupExpiredRoomsJob] Closed {expiredRooms.Count} expired rooms.");
        }
    }
}
