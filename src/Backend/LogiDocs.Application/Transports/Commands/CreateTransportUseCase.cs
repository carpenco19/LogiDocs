using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Transports;
using LogiDocs.Domain.Entities;

namespace LogiDocs.Application.Transports.Commands;

public sealed class CreateTransportUseCase
{
    private readonly ILogiDocsDbContext _db;

    public CreateTransportUseCase(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> ExecuteAsync(CreateTransportRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ReferenceNo))
            throw new ArgumentException("ReferenceNo is required.");

        if (string.IsNullOrWhiteSpace(req.Origin))
            throw new ArgumentException("Origin is required.");

        if (string.IsNullOrWhiteSpace(req.Destination))
            throw new ArgumentException("Destination is required.");

        var refNo = req.ReferenceNo.Trim();

        // IMPORTANT: fără AnyAsync (EF Core), folosim Any() (LINQ)
        var exists = _db.Transports.Any(x => x.ReferenceNo == refNo);
        if (exists)
            throw new InvalidOperationException("ReferenceNo already exists.");

        var transport = new Transport
        {
            ReferenceNo = refNo,
            Origin = req.Origin.Trim(),
            Destination = req.Destination.Trim(),
            CreatedByUserId = req.CreatedByUserId
        };

        _db.Add(transport);
        await _db.SaveChangesAsync(ct);

        return transport.Id;
    }
}