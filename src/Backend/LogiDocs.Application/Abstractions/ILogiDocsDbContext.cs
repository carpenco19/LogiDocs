using LogiDocs.Domain.Entities;
using System.Collections.Generic;



namespace LogiDocs.Application.Abstractions;

public interface ILogiDocsDbContext
{
    IQueryable<Transport> Transports { get; }
    IQueryable<Document> Documents { get; }
    IQueryable<AuditEntry> AuditEntries { get; }
    IQueryable<CustomsPayment> CustomsPayments { get; }


    void Add<T>(T entity) where T : class;
    void Delete<T>(T entity) where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}