using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Transports;
using LogiDocs.Domain.Entities;
using LogiDocs.Domain.Enums;

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

        if (req.CreatedByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.");

        var refNo = req.ReferenceNo.Trim();

        var exists = _db.Transports.Any(x => x.ReferenceNo == refNo);
        if (exists)
            throw new InvalidOperationException("ReferenceNo already exists.");

        var segments = BuildSegments(req);

        var transport = new Transport
        {
            ReferenceNo = refNo,
            Origin = req.Origin.Trim(),
            Destination = req.Destination.Trim(),
            CreatedByUserId = req.CreatedByUserId,
            Segments = segments
        };

        _db.Add(transport);
        await _db.SaveChangesAsync(ct);

        return transport.Id;
    }

    private static List<TransportSegment> BuildSegments(CreateTransportRequest req)
    {
        if (req.Segments == null || req.Segments.Count == 0)
        {
            return new List<TransportSegment>
            {
                new()
                {
                    OrderNo = 1,
                    Mode = TransportMode.Unspecified,
                    Origin = req.Origin.Trim(),
                    Destination = req.Destination.Trim()
                }
            };
        }

        var orderedSegments = req.Segments
            .OrderBy(x => x.OrderNo <= 0 ? int.MaxValue : x.OrderNo)
            .ToList();

        var result = new List<TransportSegment>();

        for (int i = 0; i < orderedSegments.Count; i++)
        {
            var segment = orderedSegments[i];

            if (string.IsNullOrWhiteSpace(segment.Origin))
                throw new ArgumentException($"Segment origin is required at position {i + 1}.");

            if (string.IsNullOrWhiteSpace(segment.Destination))
                throw new ArgumentException($"Segment destination is required at position {i + 1}.");

            if (!Enum.IsDefined(typeof(TransportMode), segment.Mode))
                throw new ArgumentException($"Segment mode is invalid at position {i + 1}.");

            result.Add(new TransportSegment
            {
                OrderNo = i + 1,
                Mode = (TransportMode)segment.Mode,
                Origin = segment.Origin.Trim(),
                Destination = segment.Destination.Trim(),
                OperatorName = string.IsNullOrWhiteSpace(segment.OperatorName)
                    ? null
                    : segment.OperatorName.Trim()
            });
        }

        return result;
    }
}