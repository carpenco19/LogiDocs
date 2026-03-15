using System.Security.Cryptography;
using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Documents;
using LogiDocs.Domain.Enums;

namespace LogiDocs.Application.Documents.Queries;

public sealed class VerifyDocumentUseCase
{
    private readonly ILogiDocsDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IAuditWriter _audit;

    public VerifyDocumentUseCase(
        ILogiDocsDbContext db,
        IDocumentStorage storage,
        IAuditWriter audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<DocumentVerificationDto> ExecuteAsync(
        Guid documentId,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default)
    {
        var doc = _db.Documents.FirstOrDefault(d => d.Id == documentId);

        if (doc == null)
            throw new InvalidOperationException("Document not found.");

        if (string.IsNullOrWhiteSpace(doc.StoredRelativePath))
            throw new InvalidOperationException("Stored file path is missing.");

        await using var stream = await _storage.OpenReadAsync(doc.StoredRelativePath, ct);

        if (stream.CanSeek)
            stream.Position = 0;

        string recalculatedSha256;
        using (var sha = SHA256.Create())
        {
            var hash = await sha.ComputeHashAsync(stream, ct);
            recalculatedSha256 = Convert.ToHexString(hash);
        }

        var hashMatches = string.Equals(
            recalculatedSha256,
            doc.Sha256,
            StringComparison.OrdinalIgnoreCase);

        if (!hashMatches && doc.Status != DocumentStatus.Tampered)
        {
            doc.Status = DocumentStatus.Tampered;
            await _db.SaveChangesAsync(ct);
        }

        var isRegistered =
            !string.IsNullOrWhiteSpace(doc.BlockchainTxId) &&
            string.Equals(doc.ChainStatus?.ToString(), "Registered", StringComparison.OrdinalIgnoreCase) &&
            doc.RegisteredOnChainAtUtc.HasValue;

        var isIntegrityValid = isRegistered && hashMatches;

        var message = !isRegistered
            ? "Documentul nu este înregistrat pe blockchain."
            : hashMatches
                ? "Integritatea documentului este validă."
                : "Documentul a fost modificat. Hash-ul recalculat nu corespunde cu cel salvat.";

        await _audit.WriteAsync(
            entityType: "Document",
            entityId: doc.Id,
            action: "DocumentVerified",
            details: $"Document {doc.OriginalFileName} verified. RegisteredOnChain: {isRegistered}. HashMatches: {hashMatches}. Status: {doc.Status}.",
            performedByUserId: performedByUserId,
            performedByName: performedByName,
            performedByRole: performedByRole,
            ct: ct);

        return new DocumentVerificationDto
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
    }
}