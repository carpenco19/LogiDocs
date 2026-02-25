using LogiDocs.Application.Documents.Commands;
using Microsoft.AspNetCore.Mvc;

namespace LogiDocs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentUseCase _uploadUseCase;

    public DocumentsController(UploadDocumentUseCase uploadUseCase)
    {
        _uploadUseCase = uploadUseCase;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] Guid transportId,
        [FromForm] int type,
        [FromForm] Guid uploadedByUserId,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        await using var stream = file.OpenReadStream();

        var documentId = await _uploadUseCase.ExecuteAsync(
            transportId,
            type,
            uploadedByUserId,
            stream,
            file.FileName,
            ct);

        return Ok(new { documentId });
    }
}