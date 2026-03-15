namespace LogiDocs.Infrastructure.Blockchain;

public sealed class BlockchainOptions
{
    public const string SectionName = "Blockchain";

    public string Provider { get; set; } = "Solana";
    public string SolanaRpcUrl { get; set; } = string.Empty;
    public string ProgramId { get; set; } = string.Empty;
    public string WalletPath { get; set; } = string.Empty;
    public string WslProjectPath { get; set; } = string.Empty;
}