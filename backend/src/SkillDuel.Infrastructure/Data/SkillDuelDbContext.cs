using Microsoft.EntityFrameworkCore;
using SkillDuel.Domain.Entities;
using SkillDuel.Infrastructure.Configurations;

namespace SkillDuel.Infrastructure.Data;

public class SkillDuelDbContext : DbContext
{
    public SkillDuelDbContext(DbContextOptions<SkillDuelDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<GameRound> GameRounds { get; set; } = null!;
    public DbSet<PlayerAnswer> PlayerAnswers { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomPlayer> RoomPlayers { get; set; } = null!;
    public DbSet<UserCategoryStat> UserCategoryStats { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkillDuelDbContext).Assembly);

        modelBuilder.Entity<User>().HasIndex(u => u.Username);
        modelBuilder.Entity<User>().HasIndex(u => u.Email);

        modelBuilder.Entity<GameSession>().HasIndex(g => g.Player1Id);
        modelBuilder.Entity<GameSession>().HasIndex(g => g.Player2Id);
        modelBuilder.Entity<GameSession>().HasIndex(g => g.Status);

        modelBuilder.Entity<Question>().HasIndex(q => q.Status);
        modelBuilder.Entity<Question>().HasIndex(q => q.CategoryId);
        modelBuilder.Entity<Question>().HasIndex(q => q.DifficultyLevel);

        modelBuilder.Entity<Room>().HasIndex(r => r.Code);
        modelBuilder.Entity<Room>().HasIndex(r => r.Status);

        modelBuilder.Entity<Friendship>().HasIndex(f => f.UserId);
        modelBuilder.Entity<Friendship>().HasIndex(f => f.FriendId);
    }
}
