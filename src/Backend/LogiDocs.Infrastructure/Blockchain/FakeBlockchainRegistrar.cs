using LogiDocs.Application.Abstractions;

namespace LogiDocs.Infrastructure.Blockchain;

public sealed class FakeBlockchainRegistrar : IBlockchainRegistrar
{
    public Task<string> RegisterDocumentHashAsync(string sha256, Guid documentId, CancellationToken ct)
    {
        // Simulăm un TxId ca înainte
        return Task.FromResult("SIMULATED_TX_" + Guid.NewGuid().ToString("N"));
    }
}