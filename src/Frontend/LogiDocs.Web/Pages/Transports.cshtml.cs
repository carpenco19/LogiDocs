using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

[Authorize] // toți autentificați pot vedea lista
public sealed class TransportsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public string? Error { get; set; }
    public List<TransportRow>? Items { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");
            Items = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");
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