using LogiDocs.Api.Security;
using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Contracts.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        var userIdText = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdText, out var userId))
            return Forbid();

        req.CreatedByUserId = userId;

        var id = await uc.ExecuteAsync(req, ct);
        return Ok(id);
    }
}