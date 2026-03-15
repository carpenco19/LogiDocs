namespace LogiDocs.Application.Abstractions;

public interface IBlockchainRegistrar
{
    Task<BlockchainRegistrationResult> RegisterDocumentHashAsync(
        string sha256,
        Guid documentId,
        Guid transportId,
        CancellationToken ct);
}