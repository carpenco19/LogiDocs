using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Documents;

namespace LogiDocs.Application.Documents.Queries;

public sealed class VerifyDocumentUseCase
{
    private readonly ILogiDocsDbContext _db;

    public VerifyDocumentUseCase(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public Task<DocumentVerificationDto> ExecuteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = _db.Documents
            .FirstOrDefault(d => d.Id == documentId);

        if (doc == null)
            throw new InvalidOperationException("Document not found.");

        var isRegistered =
            !string.IsNullOrWhiteSpace(doc.BlockchainTxId) &&
            string.Equals(doc.ChainStatus?.ToString(), "Registered", StringComparison.OrdinalIgnoreCase) &&
            doc.RegisteredOnChainAtUtc.HasValue;

        var isIntegrityValid =
            isRegistered &&
            !string.IsNullOrWhiteSpace(doc.Sha256);

        var message = !isRegistered
            ? "Documentul nu este înregistrat pe blockchain."
            : isIntegrityValid
                ? "Integritatea documentului este validă."
                : "Documentul nu a trecut verificarea de integritate.";

        var result = new DocumentVerificationDto
        {
            DocumentId = doc.Id,
            OriginalFileName = doc.OriginalFileName,
            Sha256 = doc.Sha256,
            BlockchainTxId = doc.BlockchainTxId,
            ChainStatus = doc.ChainStatus?.ToString(),
            RegisteredOnChainAtUtc = doc.RegisteredOnChainAtUtc,
            IsRegisteredOnChain = isRegistered,
            IsIntegrityValid = isIntegrityValid,
            VerificationMessage = message
        };

        return Task.FromResult(result);
    }
}