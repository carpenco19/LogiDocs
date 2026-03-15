using LogiDocs.Api.Security;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Contracts.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogiDocs.Api.Controllers;

[ApiController]
[Route("api/transports")]
[Authorize]
public sealed class TransportsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = ApiRoles.AllOperational)]
    public async Task<ActionResult<List<TransportDto>>> GetAll(
        [FromServices] GetTransportsUseCase uc,
        CancellationToken ct)
    {
        var items = await uc.ExecuteAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = ApiRoles.CreateTransport)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateTransportRequest req,
        [FromServices] CreateTransportUseCase uc,
        CancellationToken ct)
    {
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdText, out var userId))
            return Forbid();

        req.CreatedByUserId = userId;

        var performedByName =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name);

        var performedByRole = User.FindFirstValue(ClaimTypes.Role);

        var id = await uc.ExecuteAsync(req, performedByName, performedByRole, ct);
        return Ok(id);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ApiRoles.Administrator)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteTransportUseCase uc,
        CancellationToken ct)
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

        await uc.ExecuteAsync(id, performedByUserId, performedByName, performedByRole, ct);
        return NoContent();
    }
}