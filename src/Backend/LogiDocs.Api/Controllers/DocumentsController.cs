using System.Security.Claims;
using LogiDocs.Api.Security;
using LogiDocs.Application.Abstractions;
using LogiDocs.Application.Documents.Commands;
using LogiDocs.Application.Documents.Queries;
using LogiDocs.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Api.Controllers;

public sealed class UploadDocumentForm
{
    public Guid TransportId { get; set; }
    public int Type { get; set; }

    // Se păstrează doar ca să nu rupem imediat request-urile vechi.
    // Backend-ul nu îl mai folosește pentru securitate.
    public Guid UploadedByUserId { get; set; }

    public IFormFile File { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentUseCase _uploadUseCase;
    private readonly GetDocumentsByTransportUseCase _getByTransport;
    private readonly DownloadDocumentUseCase _downloadUseCase;
    private readonly RegisterDocumentOnChainUseCase _registerOnChainUseCase;
    private readonly VerifyDocumentUseCase _verifyDocumentUseCase;
    private readonly ILogiDocsDbContext _db;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        GetDocumentsByTransportUseCase getByTransport,
        DownloadDocumentUseCase downloadUseCase,
        RegisterDocumentOnChainUseCase registerOnChainUseCase,
        VerifyDocumentUseCase verifyDocumentUseCase,
        ILogiDocsDbContext db)
    {
        _uploadUseCase = uploadUseCase;
        _getByTransport = getByTransport;
        _downloadUseCase = downloadUseCase;
        _registerOnChainUseCase = registerOnChainUseCase;
        _verifyDocumentUseCase = verifyDocumentUseCase;
        _db = db;
    }

    [HttpGet("by-transport/{transportId:guid}")]
    [Authorize(Roles = ApiRoles.AllOperational)]
    public async Task<IActionResult> GetByTransport(Guid transportId, CancellationToken ct)
    {
        var items = await _getByTransport.ExecuteAsync(transportId, ct);
        return Ok(items);
    }

    [HttpGet("{documentId:guid}/download")]
    [Authorize(Roles = ApiRoles.AllOperational)]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken ct)
    {
        var (stream, fileName) = await _downloadUseCase.ExecuteAsync(documentId, ct);
        return File(stream, "application/octet-stream", fileName);
    }

    [HttpGet("{documentId:guid}/verify")]
    [Authorize(Roles = ApiRoles.ReviewDocuments)]
    public async Task<IActionResult> Verify(Guid documentId, CancellationToken ct)
    {
        try
        {
            var result = await _verifyDocumentUseCase.ExecuteAsync(documentId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = ApiRoles.UploadDocuments)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentForm form, CancellationToken ct)
    {
        if (form.File == null || form.File.Length == 0)
            return BadRequest("File is required.");

        if (form.TransportId == Guid.Empty)
            return BadRequest("TransportId is required.");

        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdText, out var uploadedByUserId))
            return Forbid();

        await using var stream = form.File.OpenReadStream();

        var documentId = await _uploadUseCase.ExecuteAsync(
            form.TransportId,
            form.Type,
            uploadedByUserId,
            stream,
            form.File.FileName,
            ct);

        return Ok(new { documentId });
    }

    [HttpPost("{documentId:guid}/register-onchain")]
    [Authorize(Roles = ApiRoles.RegisterOnChain)]
    public async Task<IActionResult> RegisterOnChain(Guid documentId, CancellationToken ct)
    {
        var existingDoc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (existingDoc == null)
            return NotFound("Document not found.");

        await _registerOnChainUseCase.ExecuteAsync(documentId, ct);

        var doc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (doc == null)
            return NotFound("Document not found after registration.");

        return Ok(new
        {
            documentId = doc.Id,
            chainStatus = doc.ChainStatus?.ToString(),
            blockchainTxId = doc.BlockchainTxId,
            registeredOnChainAtUtc = doc.RegisteredOnChainAtUtc,
            chainError = doc.ChainError
        });
    }

    [HttpPost("{documentId:guid}/validate")]
    [Authorize(Roles = ApiRoles.ValidateDocuments)]
    public async Task<IActionResult> Validate(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.Documents
            .Include(x => x.Transport)
            .FirstOrDefaultAsync(x => x.Id == documentId, ct);

        if (doc == null)
            return NotFound();

        doc.Status = DocumentStatus.Verified;

        await _db.SaveChangesAsync(ct);

        var allDocs = await _db.Documents
            .Where(x => x.TransportId == doc.TransportId)
            .ToListAsync(ct);

        var allVerified = allDocs.All(x => x.Status == DocumentStatus.Verified);

        if (allVerified)
        {
            var transport = await _db.Transports
                .FirstOrDefaultAsync(x => x.Id == doc.TransportId, ct);

            if (transport != null)
            {
                transport.Status = TransportStatus.Completed;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(new
        {
            documentId = doc.Id,
            status = doc.Status.ToString()
        });
    }

    [HttpPost("{documentId:guid}/reject")]
    [Authorize(Roles = ApiRoles.ValidateDocuments)]
    public async Task<IActionResult> Reject(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == documentId, ct);

        if (doc == null)
            return NotFound("Document not found.");

        doc.Status = DocumentStatus.Rejected;

        await _db.SaveChangesAsync(ct);

        var transport = await _db.Transports.FirstOrDefaultAsync(x => x.Id == doc.TransportId, ct);

        if (transport != null && transport.Status == TransportStatus.Completed)
        {
            transport.Status = TransportStatus.InProcess;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            documentId = doc.Id,
            status = doc.Status.ToString()
        });
    }
}