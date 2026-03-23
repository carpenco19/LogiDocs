using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

[Authorize]
public sealed class TransportsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public string? Error { get; set; }
    public string? Success { get; set; }

    public List<TransportRow> Items { get; set; } = new();
    public bool ShowCustomsPaymentColumn =>
    User.IsInRole("Administrator") ||
    User.IsInRole("CustomsAuthority") ||
    User.IsInRole("CustomsBroker");

    public async Task OnGetAsync()
    {
        await LoadItemsAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            if (!User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            var client = _factory.CreateClient("LogiDocsApi");

            var response = await client.DeleteAsync($"api/transports/{id}");

            if (response.IsSuccessStatusCode)
            {
                Success = "Transport deleted successfully.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Error = "Transport was not found.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Error = "You are not allowed to delete this transport.";
            }
            else
            {
                var serverMessage = await response.Content.ReadAsStringAsync();
                Error = string.IsNullOrWhiteSpace(serverMessage)
                    ? "Failed to delete transport."
                    : serverMessage;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }

        await LoadItemsAsync();
        return Page();
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            Error = null;

            var client = _factory.CreateClient("LogiDocsApi");
            var result = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");

            Items = result ?? new List<TransportRow>();

            foreach (var transport in Items)
            {
                try
                {
                    var payment = await client.GetFromJsonAsync<CustomsPaymentRow?>(
                        $"api/transports/{transport.Id}/customs-payment");

                    if (payment is null)
                    {
                        transport.CustomsPaymentStatus = null;
                        transport.CustomsPaymentTotalAmount = null;
                        continue;
                    }

                    transport.CustomsPaymentStatus = payment.Status;
                    transport.CustomsPaymentTotalAmount = payment.TotalAmount;
                }
                catch
                {
                    transport.CustomsPaymentStatus = null;
                    transport.CustomsPaymentTotalAmount = null;
                }
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Items = new List<TransportRow>();
        }
    }

    public sealed class TransportRow
    {
        public Guid Id { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public int Status { get; set; }
        public int DocumentsCount { get; set; }
        public int VerifiedDocuments { get; set; }

        public int SegmentCount { get; set; }
        public bool IsMultimodal { get; set; }
        public string? ModesSummary { get; set; }

        public int? CustomsPaymentStatus { get; set; }
        public decimal? CustomsPaymentTotalAmount { get; set; }

        public List<TransportSegmentRow> Segments { get; set; } = new();

        public string StatusName => Status switch
        {
            0 => "Draft",
            1 => "In Process",
            2 => "Completed",
            3 => "Cancelled",
            _ => "Unknown"
        };

        public string CustomsPaymentStatusName => CustomsPaymentStatus switch
        {
            null => "No payment",
            0 => "Draft",
            1 => "Calculated",
            2 => "Paid",
            _ => "Unknown"
        };
    }

    public sealed class TransportSegmentRow
    {
        public Guid Id { get; set; }
        public int OrderNo { get; set; }
        public int Mode { get; set; }
        public string ModeName { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? OperatorName { get; set; }
    }

    public sealed class CustomsPaymentRow
    {
        public Guid Id { get; set; }
        public Guid TransportId { get; set; }

        public decimal CustomsValue { get; set; }
        public decimal DutyRate { get; set; }
        public decimal DutyAmount { get; set; }

        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }

        public decimal OtherFees { get; set; }
        public decimal TotalAmount { get; set; }

        public int Status { get; set; }

        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }

        public DateTime? CalculatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }

        public Guid CreatedByUserId { get; set; }
    }
}