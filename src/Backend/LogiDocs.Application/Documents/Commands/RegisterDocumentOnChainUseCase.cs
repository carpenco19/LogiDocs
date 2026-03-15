using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Enums;

namespace LogiDocs.Application.Documents.Commands;

public sealed class RegisterDocumentOnChainUseCase
{
    private readonly IDocumentRepository _docs;
    private readonly IBlockchainRegistrar _blockchain;
    private readonly IAuditWriter _audit;

    public RegisterDocumentOnChainUseCase(
        IDocumentRepository docs,
        IBlockchainRegistrar blockchain,
        IAuditWriter audit)
    {
        _docs = docs;
        _blockchain = blockchain;
        _audit = audit;
    }

    public async Task ExecuteAsync(
        Guid documentId,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default)
    {
        var doc = await _docs.GetByIdAsync(documentId, ct);
        if (doc == null)
            throw new InvalidOperationException("Document not found.");

        if (doc.Status != DocumentStatus.Verified)
            throw new InvalidOperationException(
                $"Only verified documents can be registered on blockchain. Current status: {doc.Status}.");

        if (!string.IsNullOrWhiteSpace(doc.BlockchainTxId))
            return;

        doc.ChainStatus = BlockchainRegistrationStatus.Pending;
        doc.ChainError = null;
        doc.RegisteredOnChainAtUtc = null;
        await _docs.SaveChangesAsync(ct);

        try
        {
            var result = await _blockchain.RegisterDocumentHashAsync(
                doc.Sha256,
                doc.Id,
                doc.TransportId,
                ct);

            doc.BlockchainTxId = result.TransactionId;
            doc.BlockchainProofAddress = result.ProofAddress;
            doc.ChainStatus = BlockchainRegistrationStatus.Registered;
            doc.RegisteredOnChainAtUtc = DateTime.UtcNow;
            doc.ChainError = null;

            await _docs.SaveChangesAsync(ct);

            await _audit.WriteAsync(
                entityType: "Document",
                entityId: doc.Id,
                action: "DocumentRegisteredOnChain",
                details: $"Document {doc.OriginalFileName} registered on chain. TxId: {doc.BlockchainTxId}. ProofAddress: {doc.BlockchainProofAddress}.",
                performedByUserId: performedByUserId,
                performedByName: performedByName,
                performedByRole: performedByRole,
                ct: ct);
        }
        catch (Exception ex)
        {
            doc.ChainStatus = BlockchainRegistrationStatus.Failed;
            doc.ChainError = ex.Message;
            await _docs.SaveChangesAsync(ct);

            await _audit.WriteAsync(
                entityType: "Document",
                entityId: doc.Id,
                action: "DocumentRegisterOnChainFailed",
                details: $"Document {doc.OriginalFileName} failed to register on chain. Error: {ex.Message}",
                performedByUserId: performedByUserId,
                performedByName: performedByName,
                performedByRole: performedByRole,
                ct: ct);

            throw;
        }
    }
}