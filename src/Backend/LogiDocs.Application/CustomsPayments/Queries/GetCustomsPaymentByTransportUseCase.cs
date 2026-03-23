using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.CustomsPayments;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Application.CustomsPayments.Queries;

public sealed class GetCustomsPaymentByTransportUseCase
{
    private readonly ILogiDocsDbContext _dbContext;

    public GetCustomsPaymentByTransportUseCase(ILogiDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomsPaymentDto?> ExecuteAsync(Guid transportId, CancellationToken ct = default)
    {
        var payment = await _dbContext.CustomsPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TransportId == transportId, ct);

        if (payment is null)
        {
            return null;
        }

        return new CustomsPaymentDto
        {
            Id = payment.Id,
            TransportId = payment.TransportId,
            CustomsValue = payment.CustomsValue,
            DutyRate = payment.DutyRate,
            DutyAmount = payment.DutyAmount,
            VatRate = payment.VatRate,
            VatAmount = payment.VatAmount,
            OtherFees = payment.OtherFees,
            TotalAmount = payment.TotalAmount,
            Status = payment.Status,
            PaymentReference = payment.PaymentReference,
            Notes = payment.Notes,
            CalculatedAtUtc = payment.CalculatedAtUtc,
            PaidAtUtc = payment.PaidAtUtc,
            CreatedByUserId = payment.CreatedByUserId
        };
    }
}