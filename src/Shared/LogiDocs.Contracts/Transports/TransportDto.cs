namespace LogiDocs.Contracts.Transports;

public sealed class TransportDto
{
    public Guid Id { get; set; }

    public string ReferenceNo { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public int Status { get; set; }

    public int DocumentsCount { get; set; }
    public int VerifiedDocuments { get; set; }

    // Multimodal summary
    public int SegmentCount { get; set; }
    public bool IsMultimodal { get; set; }
    public string ModesSummary { get; set; } = string.Empty;

    // Full segment list
    public List<TransportSegmentDto> Segments { get; set; } = new();
}