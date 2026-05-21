using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillDuel.Domain.Entities;

namespace SkillDuel.Infrastructure.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.HasKey(gs => gs.Id);

        // Player foreign keys are GUIDs, we might not have hard references in the entity to keep it thin
        // but we can add indexes
        builder.HasIndex(gs => gs.Player1Id);
        builder.HasIndex(gs => gs.Player2Id);
    }
}
