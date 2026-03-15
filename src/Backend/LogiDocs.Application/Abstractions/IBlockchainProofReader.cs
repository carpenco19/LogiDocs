namespace LogiDocs.Application.Abstractions;

public interface IBlockchainProofReader
{
    Task<BlockchainProofLookupResult> GetDocumentProofAsync(
        Guid documentId,
        CancellationToken ct);
}