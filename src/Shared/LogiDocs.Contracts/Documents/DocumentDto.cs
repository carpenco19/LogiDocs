namespace LogiDocs.Contracts.Documents;

public sealed class DocumentDto
{
    public Guid Id { get; set; }
    public Guid TransportId { get; set; }
    public int Type { get; set; }
    public int Status { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string? BlockchainTxId { get; set; }
    public string? BlockchainProofAddress { get; set; }
    public string? ChainStatus { get; set; }
    public DateTime? RegisteredOnChainAtUtc { get; set; }
    public string? ChainError { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
    
}