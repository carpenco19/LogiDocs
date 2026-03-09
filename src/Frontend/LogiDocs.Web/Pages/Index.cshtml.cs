using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using LogiDocs.Contracts.Transports;

namespace LogiDocs.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public IndexModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public int TotalTransports { get; set; }
    public int DraftTransports { get; set; }
    public int InProcessTransports { get; set; }
    public int CompletedTransports { get; set; }
    public int CancelledTransports { get; set; }

    public int TotalDocuments { get; set; }
    public int VerifiedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int TamperedDocuments { get; set; }

    public int VerificationPercent =>
        TotalDocuments == 0 ? 0 : (int)Math.Round((double)VerifiedDocuments * 100 / TotalDocuments);

    public List<TransportDto> RecentTransports { get; set; } = new();

    public async Task OnGetAsync()
    {
        var client = _factory.CreateClient("LogiDocsApi");

        var transports = await client.GetFromJsonAsync<List<TransportDto>>("api/transports")
                         ?? new List<TransportDto>();

        TotalTransports = transports.Count;
        DraftTransports = transports.Count(x => x.Status == 0);
        InProcessTransports = transports.Count(x => x.Status == 1);
        CompletedTransports = transports.Count(x => x.Status == 2);
        CancelledTransports = transports.Count(x => x.Status == 3);

        TotalDocuments = transports.Sum(x => x.DocumentsCount);
        VerifiedDocuments = transports.Sum(x => x.VerifiedDocuments);

        RejectedDocuments = 0;
        TamperedDocuments = 0;

        RecentTransports = transports
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .ToList();
    }
}