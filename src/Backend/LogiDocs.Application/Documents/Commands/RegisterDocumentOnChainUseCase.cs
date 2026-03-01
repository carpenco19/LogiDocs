using LogiDocs.Application.Abstractions;

namespace LogiDocs.Application.Documents.Commands;

public sealed class RegisterDocumentOnChainUseCase
{
    private readonly IDocumentRepository _docs;
    private readonly IBlockchainRegistrar _blockchain;

    public RegisterDocumentOnChainUseCase(IDocumentRepository docs, IBlockchainRegistrar blockchain)
    {
        _docs = docs;
        _blockchain = blockchain;
    }

    public async Task ExecuteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _docs.GetByIdAsync(documentId, ct);
        if (doc == null)
            throw new InvalidOperationException("Document not found.");

        if (!string.IsNullOrWhiteSpace(doc.BlockchainTxId))
            return;

        doc.ChainStatus = "Pending";
        doc.ChainError = null;
        doc.RegisteredOnChainAtUtc = null;
        await _docs.SaveChangesAsync(ct);

        try
        {
            var txId = await _blockchain.RegisterDocumentHashAsync(doc.Sha256, doc.Id, ct);

            doc.BlockchainTxId = txId;
            doc.ChainStatus = "Registered";
            doc.RegisteredOnChainAtUtc = DateTime.UtcNow;
            doc.ChainError = null;

            await _docs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            doc.ChainStatus = "Failed";
            doc.ChainError = ex.Message;
            await _docs.SaveChangesAsync(ct);
            throw;
        }
    }
}