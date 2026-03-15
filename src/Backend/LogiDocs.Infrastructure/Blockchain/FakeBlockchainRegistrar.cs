using LogiDocs.Application.Abstractions;

namespace LogiDocs.Infrastructure.Blockchain;

public sealed class FakeBlockchainRegistrar : IBlockchainRegistrar
{
    public Task<BlockchainRegistrationResult> RegisterDocumentHashAsync(
        string sha256,
        Guid documentId,
        Guid transportId,
        CancellationToken ct)
    {
        return Task.FromResult(new BlockchainRegistrationResult
        {
            TransactionId = "SIMULATED_TX_" + Guid.NewGuid().ToString("N"),
            ProofAddress = "SIMULATED_PROOF_" + documentId.ToString("N")
        });
    }
}