using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillDuel.Domain.Entities;
using System.Text.Json;

namespace SkillDuel.Infrastructure.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        
        // Options column as PostgreSQL text array
        builder.Property(q => q.Options)
            .HasColumnType("text[]");

        builder.HasOne(q => q.Category)
            .WithMany()
            .HasForeignKey(q => q.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
