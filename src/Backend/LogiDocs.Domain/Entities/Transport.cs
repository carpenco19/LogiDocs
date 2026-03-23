using LogiDocs.Domain.Enums;

namespace LogiDocs.Domain.Entities;

public sealed class Transport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ReferenceNo { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public TransportStatus Status { get; set; } = TransportStatus.Draft;

    public Guid CreatedByUserId { get; set; }

    public List<Document> Documents { get; set; } = new();

    public List<TransportSegment> Segments { get; set; } = new();
    public CustomsPayment? CustomsPayment { get; set; }
}