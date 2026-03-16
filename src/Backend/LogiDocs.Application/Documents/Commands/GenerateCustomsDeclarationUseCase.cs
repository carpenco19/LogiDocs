using System.Security.Cryptography;
using System.Text;
using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Entities;
using LogiDocs.Domain.Enums;

namespace LogiDocs.Application.Documents.Commands;

public sealed class GenerateCustomsDeclarationUseCase
{
    private readonly ILogiDocsDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IAuditWriter _audit;

    public GenerateCustomsDeclarationUseCase(
        ILogiDocsDbContext db,
        IDocumentStorage storage,
        IAuditWriter audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<Guid> ExecuteAsync(
        Guid transportId,
        Guid generatedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct = default)
    {
        var transport = _db.Transports.FirstOrDefault(x => x.Id == transportId);

        if (transport == null)
            throw new InvalidOperationException("Transport not found.");

        var allDocs = _db.Documents
            .Where(x => x.TransportId == transportId)
            .ToList();

        var latestDocsByType = allDocs
            .GroupBy(x => x.Type)
            .Select(g => g.OrderByDescending(x => x.UploadedAtUtc).First())
            .OrderBy(x => x.Type)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("CUSTOMS DECLARATION");
        sb.AppendLine("===================");
        sb.AppendLine($"Transport reference: {transport.ReferenceNo}");
        sb.AppendLine($"Origin: {transport.Origin}");
        sb.AppendLine($"Destination: {transport.Destination}");
        sb.AppendLine($"Generated at (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("Latest documents by type:");
        if (latestDocsByType.Count == 0)
        {
            sb.AppendLine("- No documents available");
        }
        else
        {
            foreach (var item in latestDocsByType)
            {
                sb.AppendLine(
                    $"- Type: {item.Type} | File: {item.OriginalFileName} | Status: {item.Status} | Uploaded: {item.UploadedAtUtc:yyyy-MM-dd HH:mm:ss}");
            }
        }

        var fileContent = sb.ToString();
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        await using var memoryStream = new MemoryStream(fileBytes);

        string sha256;
        using (var sha = SHA256.Create())
        {
            memoryStream.Position = 0;
            var hash = await sha.ComputeHashAsync(memoryStream, ct);
            sha256 = Convert.ToHexString(hash);
        }

        memoryStream.Position = 0;

        var originalFileName =
            $"customs_declaration_{transport.ReferenceNo}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

        var (storedFileName, relativePath) = await _storage.SaveAsync(
            transportId,
            originalFileName,
            memoryStream,
            ct);

        var declarationDocument = new Document
        {
            Id = Guid.NewGuid(),
            TransportId = transportId,
            Type = DocumentType.CustomsDeclaration,
            Status = DocumentStatus.Uploaded,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoredRelativePath = relativePath,
            Sha256 = sha256,
            BlockchainTxId = null,
            BlockchainProofAddress = null,
            ChainStatus = null,
            ChainError = null,
            RegisteredOnChainAtUtc = null,
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = generatedByUserId
        };

        _db.Add(declarationDocument);

        if (transport.Status == TransportStatus.Draft)
        {
            transport.Status = TransportStatus.InProcess;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            entityType: "Document",
            entityId: declarationDocument.Id,
            action: "CustomsDeclarationGenerated",
            details: $"Customs declaration {declarationDocument.OriginalFileName} generated for transport {transport.ReferenceNo}.",
            performedByUserId: generatedByUserId,
            performedByName: performedByName,
            performedByRole: performedByRole,
            ct: ct);

        return declarationDocument.Id;
    }
}