using LogiDocs.Application.Abstractions;

namespace LogiDocs.Application.Transports.Commands;

public sealed class DeleteTransportUseCase
{
    private readonly ILogiDocsDbContext _db;
    private readonly IAuditWriter _audit;

    public DeleteTransportUseCase(ILogiDocsDbContext db, IAuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task ExecuteAsync(
        Guid transportId,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default)
    {
        var transport = _db.Transports.FirstOrDefault(x => x.Id == transportId);

        if (transport is null)
            throw new InvalidOperationException("Transport not found.");

        var details =
            $"Transport {transport.ReferenceNo} deleted. Route: {transport.Origin} -> {transport.Destination}.";

        _db.Delete(transport);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            entityType: "Transport",
            entityId: transportId,
            action: "TransportDeleted",
            details: details,
            performedByUserId: performedByUserId,
            performedByName: performedByName,
            performedByRole: performedByRole,
            ct: ct);
    }
}