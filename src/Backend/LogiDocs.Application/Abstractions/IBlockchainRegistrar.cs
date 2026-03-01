namespace LogiDocs.Application.Abstractions;

public interface IBlockchainRegistrar
{
    /// Returnează semnătura/TxId (signature) de pe Solana.
    Task<string> RegisterDocumentHashAsync(string sha256, Guid documentId, CancellationToken ct);
}