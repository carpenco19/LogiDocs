namespace LogiDocs.Contracts.Documents;

public sealed class DocumentDto
{
    public Guid Id { get; set; }
    public Guid TransportId { get; set; }
    public int Type { get; set; }     // DocumentType ca int (Contracts nu depinde de Domain)
    public int Status { get; set; }   // DocumentStatus ca int
    public string OriginalFileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string? BlockchainTxId { get; set; }
    public string? ChainStatus { get; set; }
    public DateTime? RegisteredOnChainAtUtc { get; set; }
    public string? ChainError { get; set; } // opțional, dar util
    public DateTime UploadedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
}