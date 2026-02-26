using LogiDocs.Application.Abstractions;

namespace LogiDocs.Application.Documents.Queries;

public sealed class DownloadDocumentUseCase
{
    private readonly ILogiDocsDbContext _db;
    private readonly IDocumentStorage _storage;

    public DownloadDocumentUseCase(ILogiDocsDbContext db, IDocumentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<(Stream Stream, string FileName)> ExecuteAsync(Guid documentId, CancellationToken ct)
    {
        // IQueryable -> luăm un singur document
        var doc = _db.Documents.FirstOrDefault(x => x.Id == documentId);

        if (doc is null)
            throw new InvalidOperationException("Document not found.");

        var relativePath = doc.StoredRelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new InvalidOperationException("StoredRelativePath missing.");

        var stream = await _storage.OpenReadAsync(relativePath, ct);

        var fileName = string.IsNullOrWhiteSpace(doc.OriginalFileName)
            ? $"{doc.Id}.bin"
            : doc.OriginalFileName;

        return (stream, fileName);
    }
}