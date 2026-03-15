namespace LogiDocs.Application.Abstractions;

public sealed class BlockchainProofLookupResult
{
    public bool Exists { get; init; }
    public string? ProofAddress { get; init; }
    public string? DocumentIdHex { get; init; }
    public string? TransportIdHex { get; init; }
    public string? DocumentHashHex { get; init; }
    public string? RegisteredBy { get; init; }
    public long? RegisteredAtUnix { get; init; }
}