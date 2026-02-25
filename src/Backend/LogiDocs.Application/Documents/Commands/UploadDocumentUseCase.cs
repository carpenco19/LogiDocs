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
        int type,                 // vine din API
        Guid uploadedByUserId,
        Stream fileStream,
        string originalFileName,
        CancellationToken ct = default)
    {
        var transportExists = _db.Transports.Any(x => x.Id == transportId);
        if (!transportExists)
            throw new InvalidOperationException("Transport not found.");

        // 1) SHA256
        fileStream.Position = 0;
        string sha256;
        using (var sha = SHA256.Create())
        {
            var hash = await sha.ComputeHashAsync(fileStream, ct);
            sha256 = Convert.ToHexString(hash); // 64 chars
        }

        // 2) Save file
        fileStream.Position = 0;
        var (storedFileName, relativePath) = await _storage.SaveAsync(
            transportId, originalFileName, fileStream, ct);

        // 3) Save DB row
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
        await _db.SaveChangesAsync(ct);

        return doc.Id;
    }
}