namespace LogiDocs.Contracts.Transports;

public sealed class CreateTransportRequest
{
    public string ReferenceNo { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
}