using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LogiDocs.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogiDocs.Infrastructure.Blockchain;

public sealed class SolanaBlockchainProofReader : IBlockchainProofReader
{
    private readonly BlockchainOptions _options;
    private readonly ILogger<SolanaBlockchainProofReader> _logger;

    public SolanaBlockchainProofReader(
        IOptions<BlockchainOptions> options,
        ILogger<SolanaBlockchainProofReader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BlockchainProofLookupResult> GetDocumentProofAsync(
        Guid documentId,
        CancellationToken ct)
    {
        ValidateOptions();

        var documentIdHex = ToHex(SolanaEncoding.GuidTo16Bytes(documentId));

        var command =
            $"cd {EscapeBashValue(_options.WslProjectPath)} && " +
            $"ANCHOR_PROVIDER_URL={EscapeBashValue(_options.SolanaRpcUrl)} " +
            $"ANCHOR_WALLET={EscapeBashValue(_options.WalletPath)} " +
            $"node scripts/read-document-proof.js {documentIdHex}";

        _logger.LogInformation(
            "Starting Solana proof lookup for document {DocumentId}.",
            documentId);

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
                    ? "Solana proof lookup failed."
                    : stdErr.Trim();

                _logger.LogError(
                    "Solana proof lookup failed for document {DocumentId}. ExitCode={ExitCode}. Error={Error}",
                    documentId,
                    process.ExitCode,
                    message);

                throw new InvalidOperationException(message);
            }

            if (string.IsNullOrWhiteSpace(stdOut))
                throw new InvalidOperationException("Solana proof lookup returned empty output.");

            var result = JsonSerializer.Deserialize<SolanaProofScriptResult>(
                stdOut.Trim(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
                throw new InvalidOperationException("Solana proof lookup returned invalid JSON result.");

            _logger.LogInformation(
                "Solana proof lookup completed for document {DocumentId}. Exists={Exists}, ProofAddress={ProofAddress}",
                documentId,
                result.Exists,
                result.ProofAddress);

            return new BlockchainProofLookupResult
            {
                Exists = result.Exists,
                ProofAddress = result.ProofAddress,
                DocumentIdHex = result.DocumentIdHex,
                TransportIdHex = result.TransportIdHex,
                DocumentHashHex = result.DocumentHashHex,
                RegisteredBy = result.RegisteredBy,
                RegisteredAtUnix = result.RegisteredAtUnix
            };
        }
        catch (Win32Exception ex)
        {
            _logger.LogError(ex, "wsl.exe could not be started for Solana proof lookup.");
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

    private sealed class SolanaProofScriptResult
    {
        public bool Exists { get; set; }
        public string? ProofAddress { get; set; }
        public string? DocumentIdHex { get; set; }
        public string? TransportIdHex { get; set; }
        public string? DocumentHashHex { get; set; }
        public string? RegisteredBy { get; set; }
        public long? RegisteredAtUnix { get; set; }
    }
}