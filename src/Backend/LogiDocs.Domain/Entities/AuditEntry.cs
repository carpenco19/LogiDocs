namespace LogiDocs.Domain.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }

    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? PerformedByRole { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}