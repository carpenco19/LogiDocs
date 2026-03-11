using LogiDocs.Domain.Enums;

namespace LogiDocs.Domain.Entities;

public sealed class TransportSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransportId { get; set; }
    public Transport? Transport { get; set; }

    public int OrderNo { get; set; }

    public TransportMode Mode { get; set; } = TransportMode.Unspecified;

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public string? OperatorName { get; set; }
}