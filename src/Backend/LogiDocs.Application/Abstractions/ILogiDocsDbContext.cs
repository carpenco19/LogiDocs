using LogiDocs.Domain.Entities;

namespace LogiDocs.Application.Abstractions;

public interface ILogiDocsDbContext
{
    IQueryable<Transport> Transports { get; }
    IQueryable<Document> Documents { get; }

    void Add<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}