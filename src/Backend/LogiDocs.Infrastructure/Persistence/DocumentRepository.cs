using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Infrastructure.Persistence;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly LogiDocsDbContext _db;

    public DocumentRepository(LogiDocsDbContext db)
    {
        _db = db;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Documents.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}