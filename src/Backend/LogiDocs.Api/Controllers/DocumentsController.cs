using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using Microsoft.AspNetCore.Mvc;

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
    private readonly DownloadDocumentUseCase _downloadUseCase;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        GetDocumentsByTransportUseCase getByTransport,
        DownloadDocumentUseCase downloadUseCase)
    {
        _uploadUseCase = uploadUseCase;
        _getByTransport = getByTransport;
        _downloadUseCase = downloadUseCase;
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
        var (stream, fileName) = await _downloadUseCase.ExecuteAsync(documentId, ct);
        return File(stream, "application/octet-stream", fileName);
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