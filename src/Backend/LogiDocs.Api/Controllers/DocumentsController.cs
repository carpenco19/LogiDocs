using LogiDocs.Application.Abstractions;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Api.Controllers;

public sealed class UploadDocumentForm
{
    public Guid TransportId { get; set; }
    public int Type { get; set; }
    public Guid UploadedByUserId { get; set; }
    public IFormFile File { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentUseCase _uploadUseCase;
    private readonly GetDocumentsByTransportUseCase _getByTransport;
    private readonly LogiDocsDbContext _db;
    private readonly IDocumentStorage _storage;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        GetDocumentsByTransportUseCase getByTransport,
        LogiDocsDbContext db,
        IDocumentStorage storage)
    {
        _uploadUseCase = uploadUseCase;
        _getByTransport = getByTransport;
        _db = db;
        _storage = storage;
    }

    [HttpGet("by-transport/{transportId:guid}")]
    public async Task<IActionResult> GetByTransport(Guid transportId, CancellationToken ct)
    {
        var items = await _getByTransport.ExecuteAsync(transportId, ct);
        return Ok(items);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId, ct);

        if (doc is null)
            return NotFound();

        var relativePath = doc.StoredRelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
            return Problem("StoredRelativePath is missing for this document.");

        var stream = await _storage.OpenReadAsync(relativePath, ct);

        var downloadName = string.IsNullOrWhiteSpace(doc.OriginalFileName)
            ? $"{doc.Id}.bin"
            : doc.OriginalFileName;

        return File(stream, "application/octet-stream", downloadName);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentForm form, CancellationToken ct)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest("File is required.");

        await using var stream = form.File.OpenReadStream();

        var documentId = await _uploadUseCase.ExecuteAsync(
            form.TransportId,
            form.Type,
            form.UploadedByUserId,
            stream,
            form.File.FileName,
            ct);

        return Ok(new { documentId });
    }
}