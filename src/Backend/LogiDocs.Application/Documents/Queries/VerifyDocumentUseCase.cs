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
    private readonly IBlockchainProofReader _proofReader;

    public VerifyDocumentUseCase(
        ILogiDocsDbContext db,
        IDocumentStorage storage,
        IAuditWriter audit,
        IBlockchainProofReader proofReader)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _proofReader = proofReader;
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

        var dbHashMatches = string.Equals(
            recalculatedSha256,
            doc.Sha256,
            StringComparison.OrdinalIgnoreCase);

        if (!dbHashMatches && doc.Status != DocumentStatus.Tampered)
        {
            doc.Status = DocumentStatus.Tampered;
            await _db.SaveChangesAsync(ct);
        }

        var proof = await _proofReader.GetDocumentProofAsync(doc.Id, ct);

        var isRegisteredInDb =
            !string.IsNullOrWhiteSpace(doc.BlockchainTxId) &&
            string.Equals(doc.ChainStatus?.ToString(), "Registered", StringComparison.OrdinalIgnoreCase) &&
            doc.RegisteredOnChainAtUtc.HasValue;

        var isRegisteredOnChain = proof.Exists;

        var onChainHashMatches =
            proof.Exists &&
            !string.IsNullOrWhiteSpace(proof.DocumentHashHex) &&
            string.Equals(proof.DocumentHashHex, doc.Sha256, StringComparison.OrdinalIgnoreCase);

        var isRegistered = isRegisteredInDb && isRegisteredOnChain;
        var isIntegrityValid = isRegistered && dbHashMatches && onChainHashMatches;

        string message;

        if (doc.Status == DocumentStatus.Rejected)
        {
            message = "Documentul a fost respins în sistem și nu este eligibil pentru înregistrare pe blockchain.";
        }
        else if (doc.Status == DocumentStatus.Tampered)
        {
            message = "Documentul a fost modificat, iar integritatea lui nu mai este validă.";
        }
        else if (doc.Status == DocumentStatus.Uploaded && !isRegisteredInDb)
        {
            message = "Documentul a fost încărcat în sistem, dar nu a fost încă validat pentru înregistrare pe blockchain.";
        }
        else if (doc.Status == DocumentStatus.Verified && !isRegisteredInDb)
        {
            message = "Documentul este validat în sistem, dar nu a fost încă înregistrat pe blockchain.";
        }
        else if (!isRegisteredOnChain)
        {
            message = "Documentul este marcat ca înregistrat în sistem, dar dovada on-chain nu a putut fi confirmată.";
        }
        else if (!dbHashMatches)
        {
            message = "Documentul a fost modificat. Hash-ul recalculat nu corespunde cu cel salvat.";
        }
        else if (!onChainHashMatches)
        {
            message = "Hash-ul de pe blockchain nu corespunde cu hash-ul salvat în sistem.";
        }
        else
        {
            message = "Integritatea documentului este validă și dovada on-chain a fost confirmată.";
        }

        await _audit.WriteAsync(
            entityType: "Document",
            entityId: doc.Id,
            action: "DocumentVerified",
            details:
                $"Document {doc.OriginalFileName} verified. " +
                $"RegisteredInDb: {isRegisteredInDb}. " +
                $"RegisteredOnChain: {isRegisteredOnChain}. " +
                $"DbHashMatches: {dbHashMatches}. " +
                $"OnChainHashMatches: {onChainHashMatches}. " +
                $"Status: {doc.Status}.",
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
            VerificationMessage = message,
            DocumentStatus = (int)doc.Status,
            DocumentStatusName = doc.Status.ToString()
        };
    }
}