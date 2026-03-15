using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LogiDocs.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogiDocs.Infrastructure.Blockchain;

public sealed class SolanaBlockchainRegistrar : IBlockchainRegistrar
{
    private readonly BlockchainOptions _options;
    private readonly ILogger<SolanaBlockchainRegistrar> _logger;

    public SolanaBlockchainRegistrar(
        IOptions<BlockchainOptions> options,
        ILogger<SolanaBlockchainRegistrar> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BlockchainRegistrationResult> RegisterDocumentHashAsync(
        string sha256,
        Guid documentId,
        Guid transportId,
        CancellationToken ct)
    {
        ValidateOptions();

        var payload = SolanaPayloadFactory.Create(sha256, documentId, transportId);

        var documentIdHex = ToHex(payload.DocumentId);
        var transportIdHex = ToHex(payload.TransportId);
        var documentHashHex = ToHex(payload.DocumentHash);

        var command =
            $"cd {EscapeBashValue(_options.WslProjectPath)} && " +
            $"ANCHOR_PROVIDER_URL={EscapeBashValue(_options.SolanaRpcUrl)} " +
            $"ANCHOR_WALLET={EscapeBashValue(_options.WalletPath)} " +
            $"node scripts/register-document.js {documentIdHex} {transportIdHex} {documentHashHex}";

        _logger.LogInformation(
            "Starting Solana registration for document {DocumentId} and transport {TransportId}.",
            documentId,
            transportId);

        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = $"bash -lc \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
                throw new InvalidOperationException("Failed to start wsl.exe process.");

            var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stdErrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            if (process.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(stdErr)
                    ? "Solana registration failed."
                    : stdErr.Trim();

                _logger.LogError(
                    "Solana registration failed for document {DocumentId}. ExitCode={ExitCode}. Error={Error}",
                    documentId,
                    process.ExitCode,
                    message);

                throw new InvalidOperationException(message);
            }

            if (string.IsNullOrWhiteSpace(stdOut))
                throw new InvalidOperationException("Solana registration returned empty output.");

            var result = JsonSerializer.Deserialize<SolanaScriptResult>(
                stdOut.Trim(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null || string.IsNullOrWhiteSpace(result.TransactionId))
                throw new InvalidOperationException("Solana registration returned invalid JSON result.");

            _logger.LogInformation(
                "Solana registration succeeded for document {DocumentId}. TxId={TransactionId}, ProofAddress={ProofAddress}",
                documentId,
                result.TransactionId,
                result.ProofAddress);

            return new BlockchainRegistrationResult
            {
                TransactionId = result.TransactionId,
                ProofAddress = result.ProofAddress
            };
        }
        catch (Win32Exception ex)
        {
            _logger.LogError(ex, "wsl.exe could not be started for Solana registration.");
            throw new InvalidOperationException(
                "WSL could not be started. Ensure WSL is installed and available on this machine.",
                ex);
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.SolanaRpcUrl))
            throw new InvalidOperationException("Blockchain:SolanaRpcUrl is required.");

        if (string.IsNullOrWhiteSpace(_options.ProgramId))
            throw new InvalidOperationException("Blockchain:ProgramId is required.");

        if (string.IsNullOrWhiteSpace(_options.WalletPath))
            throw new InvalidOperationException("Blockchain:WalletPath is required.");

        if (string.IsNullOrWhiteSpace(_options.WslProjectPath))
            throw new InvalidOperationException("Blockchain:WslProjectPath is required.");
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.AppendFormat("{0:x2}", b);

        return sb.ToString();
    }

    private static string EscapeBashValue(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'") + "'";
    }

    private sealed class SolanaScriptResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public string? ProofAddress { get; set; }
    }
}