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
    private readonly IAuditWriter _audit;

    public DocumentsController(
        UploadDocumentUseCase uploadUseCase,
        GetDocumentsByTransportUseCase getByTransport,
        DownloadDocumentUseCase downloadUseCase,
        RegisterDocumentOnChainUseCase registerOnChainUseCase,
        VerifyDocumentUseCase verifyDocumentUseCase,
        ILogiDocsDbContext db,
        IAuditWriter audit)
    {
        _uploadUseCase = uploadUseCase;
        _getByTransport = getByTransport;
        _downloadUseCase = downloadUseCase;
        _registerOnChainUseCase = registerOnChainUseCase;
        _verifyDocumentUseCase = verifyDocumentUseCase;
        _db = db;
        _audit = audit;
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
            var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? performedByUserId = Guid.TryParse(userIdText, out var parsedUserId)
                ? parsedUserId
                : null;

            var performedByName =
                User.FindFirstValue(ClaimTypes.Email) ??
                User.Identity?.Name ??
                User.FindFirstValue(ClaimTypes.Name);

            var performedByRole = User.FindFirstValue(ClaimTypes.Role);

            var result = await _verifyDocumentUseCase.ExecuteAsync(
                documentId,
                performedByUserId,
                performedByName,
                performedByRole,
                ct);

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

        var performedByName =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name);

        var performedByRole = User.FindFirstValue(ClaimTypes.Role);

        await using var stream = form.File.OpenReadStream();

        var documentId = await _uploadUseCase.ExecuteAsync(
            form.TransportId,
            form.Type,
            uploadedByUserId,
            performedByName,
            performedByRole,
            stream,
            form.File.FileName,
            ct);

        await RecalculateTransportStatusAsync(
            form.TransportId,
            uploadedByUserId,
            performedByName,
            performedByRole,
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

        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? performedByUserId = Guid.TryParse(userIdText, out var parsedUserId)
            ? parsedUserId
            : null;

        var performedByName =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name);

        var performedByRole = User.FindFirstValue(ClaimTypes.Role);

        try
        {
            await _registerOnChainUseCase.ExecuteAsync(
                documentId,
                performedByUserId,
                performedByName,
                performedByRole,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        await RecalculateTransportStatusAsync(
            existingDoc.TransportId,
            performedByUserId,
            performedByName,
            performedByRole,
            ct);

        var doc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (doc == null)
            return NotFound("Document not found after registration.");

        return Ok(new
        {
            documentId = doc.Id,
            chainStatus = doc.ChainStatus?.ToString(),
            blockchainTxId = doc.BlockchainTxId,
            blockchainProofAddress = doc.BlockchainProofAddress,
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

        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? performedByUserId = Guid.TryParse(userIdText, out var parsedUserId)
            ? parsedUserId
            : null;

        var performedByName =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name);

        var performedByRole = User.FindFirstValue(ClaimTypes.Role);

        doc.Status = DocumentStatus.Verified;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            entityType: "Document",
            entityId: doc.Id,
            action: "DocumentValidated",
            details: $"Document {doc.OriginalFileName} validated for transport {doc.Transport?.ReferenceNo}.",
            performedByUserId: performedByUserId,
            performedByName: performedByName,
            performedByRole: performedByRole,
            ct: ct);

        await RecalculateTransportStatusAsync(
            doc.TransportId,
            performedByUserId,
            performedByName,
            performedByRole,
            ct);

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

        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? performedByUserId = Guid.TryParse(userIdText, out var parsedUserId)
            ? parsedUserId
            : null;

        var performedByName =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name);

        var performedByRole = User.FindFirstValue(ClaimTypes.Role);

        doc.Status = DocumentStatus.Rejected;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            entityType: "Document",
            entityId: doc.Id,
            action: "DocumentRejected",
            details: $"Document {doc.OriginalFileName} was rejected.",
            performedByUserId: performedByUserId,
            performedByName: performedByName,
            performedByRole: performedByRole,
            ct: ct);

        await RecalculateTransportStatusAsync(
            doc.TransportId,
            performedByUserId,
            performedByName,
            performedByRole,
            ct);

        return Ok(new
        {
            documentId = doc.Id,
            status = doc.Status.ToString()
        });
    }

    private async Task RecalculateTransportStatusAsync(
        Guid transportId,
        Guid? performedByUserId,
        string? performedByName,
        string? performedByRole,
        CancellationToken ct)
    {
        var transport = await _db.Transports.FirstOrDefaultAsync(x => x.Id == transportId, ct);
        if (transport == null)
            return;

        var allDocs = await _db.Documents
    .Where(x => x.TransportId == transportId)
    .ToListAsync(ct);

        var oldStatus = transport.Status;

        if (allDocs.Count == 0)
        {
            transport.Status = TransportStatus.Draft;
        }
        else
        {
            var latestDocsByType = allDocs
                  .GroupBy(x => x.Type)
                  .Select(g => g
                  .OrderByDescending(x => x.UploadedAtUtc)
                   .First())
                     .ToList();

            var allLatestVerified = latestDocsByType.All(x => x.Status == DocumentStatus.Verified);
            var allLatestRegistered = latestDocsByType.All(x => x.ChainStatus == BlockchainRegistrationStatus.Registered);

            transport.Status = allLatestVerified && allLatestRegistered
                ? TransportStatus.Completed
                : TransportStatus.InProcess;
        }

        if (transport.Status != oldStatus)
        {
            await _db.SaveChangesAsync(ct);

            await _audit.WriteAsync(
                entityType: "Transport",
                entityId: transport.Id,
                action: "TransportStatusChanged",
                details: $"Transport {transport.ReferenceNo} status changed from {oldStatus} to {transport.Status}.",
                performedByUserId: performedByUserId,
                performedByName: performedByName,
                performedByRole: performedByRole,
                ct: ct);
        }
    }
}