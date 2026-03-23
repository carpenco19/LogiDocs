using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.CustomsPayments;
using LogiDocs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Application.CustomsPayments.Commands;

public sealed class MarkCustomsPaymentAsPaidUseCase
{
    private readonly ILogiDocsDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;

    public MarkCustomsPaymentAsPaidUseCase(
        ILogiDocsDbContext dbContext,
        IAuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
    }

    public async Task<CustomsPaymentDto> ExecuteAsync(
      Guid transportId,
      Guid currentUserId,
      string currentUserName,
      string currentUserRole,
      MarkCustomsPaymentAsPaidRequest request,
      CancellationToken ct = default)
    {
        var payment = await _dbContext.CustomsPayments
            .FirstOrDefaultAsync(x => x.TransportId == transportId, ct);

        if (payment is null)
        {
            throw new InvalidOperationException("Pentru acest transport nu există o plată vamală calculată.");
        }

        payment.Status = CustomsPaymentStatus.Paid;
        payment.PaidAtUtc = DateTime.UtcNow;
        payment.PaymentReference = request.PaymentReference;
        payment.Notes = request.Notes;

        await _dbContext.SaveChangesAsync(ct);

        await _auditWriter.WriteAsync(
     entityType: "CustomsPayment",
     entityId: payment.Id,
     action: "MarkedAsPaid",
     details: $"TransportId={transportId}; TotalAmount={payment.TotalAmount}; PaymentReference={payment.PaymentReference ?? "-"}; PaidAtUtc={payment.PaidAtUtc:O}",
     performedByUserId: currentUserId,
     performedByName: currentUserName,
     performedByRole: currentUserRole,
     ct: ct);

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