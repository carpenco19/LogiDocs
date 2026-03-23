namespace LogiDocs.Contracts.CustomsPayments;

public sealed class MarkCustomsPaymentAsPaidRequest
{
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}