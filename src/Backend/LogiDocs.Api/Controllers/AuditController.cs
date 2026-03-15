using LogiDocs.Api.Security;
using LogiDocs.Application.Audit.Queries;
using LogiDocs.Contracts.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiDocs.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public sealed class AuditController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = ApiRoles.Administrator)]
    public async Task<ActionResult<List<AuditEntryDto>>> GetAll(
        [FromServices] GetAuditEntriesUseCase uc,
        CancellationToken ct)
    {
        var items = await uc.ExecuteAsync(ct);
        return Ok(items);
    }
}