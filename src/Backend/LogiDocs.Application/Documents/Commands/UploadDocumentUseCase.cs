using System.Security.Cryptography;
using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Entities;
using LogiDocs.Domain.Enums;

namespace LogiDocs.Application.Documents.Commands;

public sealed class UploadDocumentUseCase
{
    private readonly ILogiDocsDbContext _db;
    private readonly IDocumentStorage _storage;

    public UploadDocumentUseCase(ILogiDocsDbContext db, IDocumentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> ExecuteAsync(
        Guid transportId,
        int type,
        Guid uploadedByUserId,
        Stream fileStream,
        string originalFileName,
        CancellationToken ct = default)
    {
        var transport = _db.Transports.FirstOrDefault(x => x.Id == transportId);
        if (transport == null)
            throw new InvalidOperationException("Transport not found.");

        fileStream.Position = 0;
        string sha256;
        using (var sha = SHA256.Create())
        {
            var hash = await sha.ComputeHashAsync(fileStream, ct);
            sha256 = Convert.ToHexString(hash);
        }

        fileStream.Position = 0;
        var (storedFileName, relativePath) = await _storage.SaveAsync(
            transportId, originalFileName, fileStream, ct);

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            TransportId = transportId,
            Type = (DocumentType)type,
            Status = DocumentStatus.Uploaded,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoredRelativePath = relativePath,
            Sha256 = sha256,
            BlockchainTxId = null,
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId
        };

        _db.Add(doc);

        if (transport.Status == TransportStatus.Draft)
        {
            transport.Status = TransportStatus.InProcess;
        }

        await _db.SaveChangesAsync(ct);

        return doc.Id;
    }
}