using LogiDocs.Contracts.Audit;
using LogiDocs.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

[Authorize(Roles = Roles.Administrator)]
public sealed class AuditHistoryModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public AuditHistoryModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [BindProperty(SupportsGet = true)]
    public string? EntityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TimeFilter { get; set; }

    public string? Error { get; set; }
    public List<AuditEntryDto> Items { get; set; } = new();
    public List<AuditEntryDto> FilteredItems { get; set; } = new();

    public int TotalEntries => Items.Count;
    public int TransportEvents => Items.Count(x => x.EntityType == "Transport");
    public int DocumentEvents => Items.Count(x => x.EntityType == "Document");
    public int TodayEvents => Items.Count(x => x.CreatedAtUtc.Date == DateTime.UtcNow.Date);

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");
            var result = await client.GetFromJsonAsync<List<AuditEntryDto>>("api/audit");
            Items = result ?? new List<AuditEntryDto>();
            FilteredItems = ApplyFilters(Items);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Items = new List<AuditEntryDto>();
            FilteredItems = new List<AuditEntryDto>();
        }
    }

    public string GetActionLabel(string action) => action switch
    {
        "TransportCreated" => "Transport created",
        "TransportDeleted" => "Transport deleted",
        "TransportCompleted" => "Transport completed",
        "TransportReturnedToInProcess" => "Returned to in process",
        "DocumentUploaded" => "Document uploaded",
        "DocumentVerified" => "Document verified",
        "DocumentValidated" => "Document validated",
        "DocumentRejected" => "Document rejected",
        "DocumentRegisteredOnChain" => "Registered on blockchain",
        "DocumentRegisterOnChainFailed" => "Blockchain registration failed",
        "MarkedAsPaid" => "Marked as paid",
        "Calculated" => "Calculated",
        _ => action
    };

    public string GetActionCssClass(string? action)
    {
        return action switch
        {
            "TransportCreated" => "action-pill action-pill--info",
            "TransportDeleted" => "action-pill action-pill--danger",
            "TransportCompleted" => "action-pill action-pill--success",
            "TransportReturnedToInProcess" => "action-pill action-pill--warning",

            "DocumentUploaded" => "action-pill action-pill--info",
            "DocumentVerified" => "action-pill action-pill--success",
            "DocumentValidated" => "action-pill action-pill--success",
            "DocumentRejected" => "action-pill action-pill--danger",
            "DocumentRegisteredOnChain" => "action-pill action-pill--success",
            "DocumentRegisterOnChainFailed" => "action-pill action-pill--danger",

            "Calculated" => "action-pill action-pill--customs-calculated",
            "MarkedAsPaid" => "action-pill action-pill--customs-paid",
            "CustomsDeclarationGenerated" => "action-pill action-pill--success",


            

            _ => "action-pill"
        };
    }

    public string ShortenDetails(string? details, int maxLength = 90)
    {
        if (string.IsNullOrWhiteSpace(details))
            return "-";

        if (details.Length <= maxLength)
            return details;

        return details.Substring(0, maxLength).TrimEnd() + "...";
    }

    private List<AuditEntryDto> ApplyFilters(List<AuditEntryDto> items)
    {
        IEnumerable<AuditEntryDto> query = items;

        if (!string.IsNullOrWhiteSpace(EntityFilter) &&
            !string.Equals(EntityFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.EntityType, EntityFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(TimeFilter) &&
            !string.Equals(TimeFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.UtcNow.Date;

            query = TimeFilter switch
            {
                "Today" => query.Where(x => x.CreatedAtUtc.Date == today),
                "Last7Days" => query.Where(x => x.CreatedAtUtc >= today.AddDays(-7)),
                _ => query
            };
        }

        return query.ToList();
    }
}