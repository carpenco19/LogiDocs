namespace LogiDocs.Infrastructure.Blockchain;

internal sealed class SolanaRegistrationPayload
{
    public byte[] DocumentId { get; init; } = Array.Empty<byte>();
    public byte[] TransportId { get; init; } = Array.Empty<byte>();
    public byte[] DocumentHash { get; init; } = Array.Empty<byte>();
}