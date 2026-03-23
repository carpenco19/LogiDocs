using LogiDocs.Domain.Enums;

namespace LogiDocs.Domain.Entities;

public sealed class CustomsPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransportId { get; set; }
    public Transport? Transport { get; set; }

    public decimal CustomsValue { get; set; }
    public decimal DutyRate { get; set; }
    public decimal DutyAmount { get; set; }

    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }

    public decimal OtherFees { get; set; }
    public decimal TotalAmount { get; set; }

    public CustomsPaymentStatus Status { get; set; } = CustomsPaymentStatus.Draft;

    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }
}