using LogiDocs.Domain.Enums;

namespace LogiDocs.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransportId { get; set; }
    public Transport? Transport { get; set; }

    public DocumentType Type { get; set; } = DocumentType.Other;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty; 
    public string StoredRelativePath { get; set; } = string.Empty;

    // integritate
    public string Sha256 { get; set; } = string.Empty;
    public string? BlockchainTxId { get; set; }

    public string? BlockchainProofAddress { get; set; }

    public DateTime? RegisteredOnChainAtUtc { get; set; } 
    public BlockchainRegistrationStatus? ChainStatus { get; set; }           // "Pending", "Registered", "Failed"
    public string? ChainError { get; set; }              



    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;


    // (opțional) - cine a încărcat documentul
    public Guid UploadedByUserId { get; set; }
}