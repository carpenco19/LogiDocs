namespace LogiDocs.Application.Abstractions;

public interface IDocumentStorage
{
    Task<(string StoredFileName, string RelativePath)> SaveAsync(
        Guid transportId,
        string originalFileName,
        Stream content,
        CancellationToken ct);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct);

    Task DeleteAsync(string relativePath, CancellationToken ct);
}