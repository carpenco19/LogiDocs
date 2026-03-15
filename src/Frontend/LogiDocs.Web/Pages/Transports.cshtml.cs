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

        public List<TransportSegmentRow> Segments { get; set; } = new();

        public string StatusName => Status switch
        {
            0 => "Draft",
            1 => "In Process",
            2 => "Completed",
            3 => "Cancelled",
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
}