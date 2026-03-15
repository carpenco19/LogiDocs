namespace LogiDocs.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(
        string entityType,
        Guid entityId,
        string action,
        string? details,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default);
}