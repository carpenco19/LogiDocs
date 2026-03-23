using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.CustomsPayments;
using LogiDocs.Domain.Entities;
using LogiDocs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Application.CustomsPayments.Commands;

public sealed class CalculateCustomsPaymentUseCase
{
    private readonly ILogiDocsDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;

    public CalculateCustomsPaymentUseCase(
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
     CalculateCustomsPaymentRequest request,
     CancellationToken ct = default)
    {
        if (request.CustomsValue < 0)
        {
            throw new InvalidOperationException("Valoarea vamală nu poate fi negativă.");
        }

        if (request.DutyRate < 0)
        {
            throw new InvalidOperationException("Cota taxei vamale nu poate fi negativă.");
        }

        if (request.VatRate < 0)
        {
            throw new InvalidOperationException("Cota TVA nu poate fi negativă.");
        }

        if (request.OtherFees < 0)
        {
            throw new InvalidOperationException("Alte taxe nu pot fi negative.");
        }

        var transportExists = await _dbContext.Transports
            .AnyAsync(x => x.Id == transportId, ct);

        if (!transportExists)
        {
            throw new InvalidOperationException("Transportul nu a fost găsit.");
        }

        var dutyAmount = Math.Round(request.CustomsValue * request.DutyRate / 100m, 2);
        var vatBase = request.CustomsValue + dutyAmount + request.OtherFees;
        var vatAmount = Math.Round(vatBase * request.VatRate / 100m, 2);
        var totalAmount = Math.Round(dutyAmount + vatAmount + request.OtherFees, 2);

        var payment = await _dbContext.CustomsPayments
            .FirstOrDefaultAsync(x => x.TransportId == transportId, ct);

        if (payment is null)
        {
            payment = new CustomsPayment
            {
                Id = Guid.NewGuid(),
                TransportId = transportId,
                CustomsValue = request.CustomsValue,
                DutyRate = request.DutyRate,
                DutyAmount = dutyAmount,
                VatRate = request.VatRate,
                VatAmount = vatAmount,
                OtherFees = request.OtherFees,
                TotalAmount = totalAmount,
                Status = CustomsPaymentStatus.Calculated,
                Notes = request.Notes,
                CalculatedAtUtc = DateTime.UtcNow,
                PaidAtUtc = null,
                CreatedByUserId = currentUserId
            };

            _dbContext.Add(payment);
        }
        else
        {
            payment.CustomsValue = request.CustomsValue;
            payment.DutyRate = request.DutyRate;
            payment.DutyAmount = dutyAmount;
            payment.VatRate = request.VatRate;
            payment.VatAmount = vatAmount;
            payment.OtherFees = request.OtherFees;
            payment.TotalAmount = totalAmount;
            payment.Status = CustomsPaymentStatus.Calculated;
            payment.Notes = request.Notes;
            payment.CalculatedAtUtc = DateTime.UtcNow;
            payment.PaidAtUtc = null;
            payment.PaymentReference = null;
        }

        await _dbContext.SaveChangesAsync(ct);

        await _auditWriter.WriteAsync(
     entityType: "CustomsPayment",
     entityId: payment.Id,
     action: "Calculated",
     details: $"TransportId={transportId}; CustomsValue={payment.CustomsValue}; DutyAmount={payment.DutyAmount}; VatAmount={payment.VatAmount}; OtherFees={payment.OtherFees}; TotalAmount={payment.TotalAmount}",
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