using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Audit;

namespace LogiDocs.Application.Audit.Queries;

public sealed class GetAuditEntriesUseCase
{
    private readonly ILogiDocsDbContext _db;

    public GetAuditEntriesUseCase(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public Task<List<AuditEntryDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var items = _db.AuditEntries
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new AuditEntryDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Action = x.Action,
                Details = x.Details,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByName = x.PerformedByName,
                PerformedByRole = x.PerformedByRole,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();

        return Task.FromResult(items);
    }
}