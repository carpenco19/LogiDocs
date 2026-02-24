using LogiDocs.Domain.Enums;

namespace LogiDocs.Contracts.Transports;

public sealed class TransportDto
{
    public Guid Id { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public TransportStatus Status { get; set; }
}