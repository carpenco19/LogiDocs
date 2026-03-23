using LogiDocs.Api.Security;
using LogiDocs.Application.CustomsPayments.Commands;
using LogiDocs.Application.CustomsPayments.Queries;
using LogiDocs.Contracts.CustomsPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogiDocs.Api.Controllers;

[ApiController]
[Route("api/transports/{transportId:guid}/customs-payment")]
[Authorize]
public sealed class CustomsPaymentsController : ControllerBase
{
    private readonly GetCustomsPaymentByTransportUseCase _getCustomsPaymentByTransportUseCase;
    private readonly CalculateCustomsPaymentUseCase _calculateCustomsPaymentUseCase;
    private readonly MarkCustomsPaymentAsPaidUseCase _markCustomsPaymentAsPaidUseCase;

    public CustomsPaymentsController(
        GetCustomsPaymentByTransportUseCase getCustomsPaymentByTransportUseCase,
        CalculateCustomsPaymentUseCase calculateCustomsPaymentUseCase,
        MarkCustomsPaymentAsPaidUseCase markCustomsPaymentAsPaidUseCase)
    {
        _getCustomsPaymentByTransportUseCase = getCustomsPaymentByTransportUseCase;
        _calculateCustomsPaymentUseCase = calculateCustomsPaymentUseCase;
        _markCustomsPaymentAsPaidUseCase = markCustomsPaymentAsPaidUseCase;
    }

    [HttpGet]
    [Authorize(Roles = $"{ApiRoles.CustomsBroker},{ApiRoles.Administrator},{ApiRoles.CustomsAuthority}")]
    public async Task<ActionResult<CustomsPaymentDto?>> GetByTransport(Guid transportId, CancellationToken ct)
    {
        var result = await _getCustomsPaymentByTransportUseCase.ExecuteAsync(transportId, ct);
        return Ok(result);
    }

    [HttpPut("calculate")]
    [Authorize(Roles = $"{ApiRoles.CustomsBroker},{ApiRoles.Administrator}")]
    public async Task<ActionResult<CustomsPaymentDto>> Calculate(
     Guid transportId,
     [FromBody] CalculateCustomsPaymentRequest request,
     CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var currentUserName =
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue(ClaimTypes.Name) ??
            "Unknown";

        var currentUserRole =
            User.FindFirstValue(ClaimTypes.Role) ??
            User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ??
            "Unknown";

        var result = await _calculateCustomsPaymentUseCase.ExecuteAsync(
            transportId,
            currentUserId,
            currentUserName,
            currentUserRole,
            request,
            ct);

        return Ok(result);
    }

    [HttpPut("mark-paid")]
    [Authorize(Roles = $"{ApiRoles.CustomsBroker},{ApiRoles.Administrator}")]
    public async Task<ActionResult<CustomsPaymentDto>> MarkAsPaid(
     Guid transportId,
     [FromBody] MarkCustomsPaymentAsPaidRequest request,
     CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var currentUserName =
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue(ClaimTypes.Name) ??
            "Unknown";

        var currentUserRole =
            User.FindFirstValue(ClaimTypes.Role) ??
            User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ??
            "Unknown";

        var result = await _markCustomsPaymentAsPaidUseCase.ExecuteAsync(
            transportId,
            currentUserId,
            currentUserName,
            currentUserRole,
            request,
            ct);

        return Ok(result);
    }
}