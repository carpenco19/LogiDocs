namespace LogiDocs.Contracts.CustomsPayments;

public sealed class CalculateCustomsPaymentRequest
{
    public decimal CustomsValue { get; set; }
    public decimal DutyRate { get; set; }
    public decimal VatRate { get; set; }
    public decimal OtherFees { get; set; }
    public string? Notes { get; set; }
}