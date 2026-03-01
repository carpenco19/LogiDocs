using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

public class TransportsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public List<TransportRow>? Items { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");
            Items = await client.GetFromJsonAsync<List<TransportRow>>("api/Transports");
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
    }
}