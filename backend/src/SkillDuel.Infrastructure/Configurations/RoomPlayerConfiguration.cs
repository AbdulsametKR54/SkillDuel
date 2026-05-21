using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillDuel.Domain.Entities;

namespace SkillDuel.Infrastructure.Configurations;

public class RoomPlayerConfiguration : IEntityTypeConfiguration<RoomPlayer>
{
    public void Configure(EntityTypeBuilder<RoomPlayer> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.HasOne(rp => rp.Room)
            .WithMany(r => r.Players)
            .HasForeignKey(rp => rp.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.User)
            .WithMany()
            .HasForeignKey(rp => rp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rp => new { rp.RoomId, rp.UserId }).IsUnique();
        builder.HasIndex(rp => new { rp.RoomId, rp.SlotNumber }).IsUnique();
    }
}
