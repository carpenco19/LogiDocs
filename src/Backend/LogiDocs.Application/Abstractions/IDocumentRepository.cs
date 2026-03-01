using LogiDocs.Domain.Entities;

namespace LogiDocs.Application.Abstractions;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}