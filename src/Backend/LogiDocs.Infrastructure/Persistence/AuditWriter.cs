using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Entities;

namespace LogiDocs.Infrastructure.Persistence;

public sealed class AuditWriter : IAuditWriter
{
    private readonly ILogiDocsDbContext _db;

    public AuditWriter(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        string entityType,
        Guid entityId,
        string action,
        string? details,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Details = details,
            PerformedByUserId = performedByUserId,
            PerformedByName = performedByName,
            PerformedByRole = performedByRole,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}