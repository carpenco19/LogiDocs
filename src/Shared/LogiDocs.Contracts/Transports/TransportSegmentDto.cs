namespace LogiDocs.Contracts.Transports;

public sealed class TransportSegmentDto
{
    public Guid Id { get; set; }

    public int OrderNo { get; set; }

    // 0 = Unspecified, 1 = Road, 2 = Rail, 3 = Sea, 4 = Air
    public int Mode { get; set; }

    public string ModeName { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public string? OperatorName { get; set; }
}