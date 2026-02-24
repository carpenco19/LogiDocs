using LogiDocs.Application.Transports.Commands;
using LogiDocs.Application.Transports.Queries;
using LogiDocs.Contracts.Transports;
using Microsoft.AspNetCore.Mvc;

namespace LogiDocs.Api.Controllers;

[ApiController]
[Route("api/transports")]
public sealed class TransportsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TransportDto>>> GetAll(
        [FromServices] GetTransportsUseCase uc,
        CancellationToken ct)
    {
        var items = await uc.ExecuteAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateTransportRequest req,
        [FromServices] CreateTransportUseCase uc,
        CancellationToken ct)
    {
        var id = await uc.ExecuteAsync(req, ct);
        return Ok(id);
    }
}