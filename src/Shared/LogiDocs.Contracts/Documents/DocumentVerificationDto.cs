namespace LogiDocs.Contracts.Documents;

public sealed class DocumentVerificationDto
{
    public Guid DocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;

    public string? BlockchainTxId { get; set; }
    public string? ChainStatus { get; set; }
    public DateTime? RegisteredOnChainAtUtc { get; set; }

    public bool IsRegisteredOnChain { get; set; }
    public bool IsIntegrityValid { get; set; }
    public string VerificationMessage { get; set; } = string.Empty;
}