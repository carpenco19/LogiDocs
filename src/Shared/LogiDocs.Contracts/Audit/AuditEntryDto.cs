namespace LogiDocs.Contracts.Audit;

public sealed class AuditEntryDto
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }

    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? PerformedByRole { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}