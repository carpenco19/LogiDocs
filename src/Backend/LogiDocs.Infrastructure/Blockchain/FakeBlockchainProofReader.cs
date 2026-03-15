using LogiDocs.Application.Abstractions;

namespace LogiDocs.Infrastructure.Blockchain;

public sealed class FakeBlockchainProofReader : IBlockchainProofReader
{
    public Task<BlockchainProofLookupResult> GetDocumentProofAsync(
        Guid documentId,
        CancellationToken ct)
    {
        return Task.FromResult(new BlockchainProofLookupResult
        {
            Exists = false
        });
    }
}