using LogiDocs.Application.Abstractions;

namespace LogiDocs.Infrastructure.Persistence.Storage;

public sealed class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public LocalDocumentStorage(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<(string StoredFileName, string RelativePath)> SaveAsync(
        Guid transportId,
        string originalFileName,
        Stream content,
        CancellationToken ct)
    {
        var safeOriginal = Path.GetFileName(originalFileName);
        var ext = Path.GetExtension(safeOriginal);
        var storedFileName = $"{Guid.NewGuid():N}{ext}";

        var relativeDir = Path.Combine("transports", transportId.ToString("N"));
        var absoluteDir = Path.Combine(_rootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var relativePath = Path.Combine(relativeDir, storedFileName);
        var absolutePath = Path.Combine(_rootPath, relativePath);

        await using var fs = new FileStream(
            absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        content.Position = 0;
        await content.CopyToAsync(fs, ct);

        return (storedFileName, relativePath.Replace("\\", "/"));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var abs = Path.Combine(_rootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(abs)) File.Delete(abs);
        return Task.CompletedTask;
    }
}