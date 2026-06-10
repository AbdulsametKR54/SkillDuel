using System;

namespace SkillDuel.Domain.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    
    public Guid ReportedUserId { get; set; }
    public User ReportedUser { get; set; } = null!;
    
    public string Reason { get; set; } = string.Empty;
    public string? ChatMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;
}
