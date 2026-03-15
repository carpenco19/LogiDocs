namespace LogiDocs.Application.Abstractions;

public sealed class BlockchainRegistrationResult
{
    public string TransactionId { get; init; } = string.Empty;
    public string? ProofAddress { get; init; }
}