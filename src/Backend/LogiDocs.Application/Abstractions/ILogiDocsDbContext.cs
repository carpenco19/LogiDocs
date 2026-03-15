using LogiDocs.Domain.Entities;

namespace LogiDocs.Application.Abstractions;

public interface ILogiDocsDbContext
{
    IQueryable<Transport> Transports { get; }
    IQueryable<Document> Documents { get; }
    IQueryable<AuditEntry> AuditEntries { get; }

    void Add<T>(T entity) where T : class;
    void Delete<T>(T entity) where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}