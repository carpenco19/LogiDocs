using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

[Authorize(Roles = "Shipper,Carrier,CustomsBroker,CustomsAuthority,Administrator")]
public sealed class TransportsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public string? Error { get; set; }
    public List<TransportRow> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var result = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");

            if (result != null)
                Items = result;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
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

        public string StatusName => Status switch
        {
            0 => "Draft",
            1 => "In Process",
            2 => "Completed",
            3 => "Cancelled",
            _ => "Unknown"
        };
    }
}