using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Transports;

namespace LogiDocs.Application.Transports.Queries;

public sealed class GetTransportsUseCase
{
    private readonly ILogiDocsDbContext _db;

    public GetTransportsUseCase(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public Task<List<TransportDto>> ExecuteAsync(CancellationToken ct = default)
    {
        
        var items = _db.Transports
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TransportDto
            {
                Id = x.Id,
                ReferenceNo = x.ReferenceNo,
                Origin = x.Origin,
                Destination = x.Destination,
                CreatedAtUtc = x.CreatedAtUtc,
                Status = (int)x.Status
            })
            .ToList();

        return Task.FromResult(items);
    }
}