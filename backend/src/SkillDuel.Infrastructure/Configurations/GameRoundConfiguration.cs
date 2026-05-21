using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillDuel.Domain.Entities;

namespace SkillDuel.Infrastructure.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.HasKey(gr => gr.Id);

        builder.HasOne(gr => gr.GameSession)
            .WithMany()
            .HasForeignKey(gr => gr.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gr => gr.Question)
            .WithMany()
            .HasForeignKey(gr => gr.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
