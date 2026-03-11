using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Transports;
using LogiDocs.Domain.Enums;

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
        var transportRows = _db.Transports
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ReferenceNo,
                x.Origin,
                x.Destination,
                x.CreatedAtUtc,
                x.Status,
                DocumentsCount = x.Documents.Count(),
                VerifiedDocuments = x.Documents.Count(d => d.Status == DocumentStatus.Verified),
                SegmentCount = x.Segments.Count()
            })
            .ToList();

        var segmentRows = _db.Transports
            .SelectMany(x => x.Segments.Select(s => new
            {
                TransportId = x.Id,
                s.Id,
                s.OrderNo,
                s.Mode,
                s.Origin,
                s.Destination,
                s.OperatorName
            }))
            .ToList();

        var segmentsByTransport = segmentRows
            .GroupBy(x => x.TransportId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.OrderNo)
                      .Select(x => new TransportSegmentDto
                      {
                          Id = x.Id,
                          OrderNo = x.OrderNo,
                          Mode = (int)x.Mode,
                          ModeName = GetModeName(x.Mode),
                          Origin = x.Origin,
                          Destination = x.Destination,
                          OperatorName = x.OperatorName
                      })
                      .ToList());

        var items = transportRows
            .Select(x =>
            {
                var segments = segmentsByTransport.TryGetValue(x.Id, out var found)
                    ? found
                    : new List<TransportSegmentDto>();

                var modesSummary = string.Join(" / ",
                    segments
                        .OrderBy(s => s.OrderNo)
                        .Select(s => s.ModeName)
                        .Distinct());

                return new TransportDto
                {
                    Id = x.Id,
                    ReferenceNo = x.ReferenceNo,
                    Origin = x.Origin,
                    Destination = x.Destination,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Status = (int)x.Status,

                    DocumentsCount = x.DocumentsCount,
                    VerifiedDocuments = x.VerifiedDocuments,

                    SegmentCount = segments.Count,
                    IsMultimodal = segments.Count > 1,
                    ModesSummary = string.IsNullOrWhiteSpace(modesSummary) ? "Unspecified" : modesSummary,
                    Segments = segments
                };
            })
            .ToList();

        return Task.FromResult(items);
    }

    private static string GetModeName(TransportMode mode) => mode switch
    {
        TransportMode.Road => "Road",
        TransportMode.Rail => "Rail",
        TransportMode.Sea => "Sea",
        TransportMode.Air => "Air",
        _ => "Unspecified"
    };
}