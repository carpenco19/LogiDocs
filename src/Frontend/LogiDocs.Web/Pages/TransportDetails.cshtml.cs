using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace LogiDocs.Web.Pages;

[Authorize] // doar utilizatori autentificați
public sealed class TransportDetailsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportDetailsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    // route param: /TransportDetails/{id}
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string? Error { get; set; }
    public string? TransportRef { get; set; }
    public List<DocumentRow> Documents { get; set; } = new();

    // ---------- Upload form ----------
    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public int Type { get; set; } // tip document (int)

    // ---------- Register form ----------
    [BindProperty]
    public Guid RegisterDocumentId { get; set; }

    // Helpers pentru UI (buton visibility)
    public bool CanUpload =>
        User.IsInRole("Shipper") || User.IsInRole("Carrier");

    public bool CanRegister =>
        User.IsInRole("CustomsBroker") || User.IsInRole("Administrator");

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            // Transport reference (MVP)
            var transports = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");
            var t = transports?.FirstOrDefault(x => x.Id == Id);
            TransportRef = t?.ReferenceNo ?? "Unknown";

            // Documents for transport
            var docs = await client.GetFromJsonAsync<List<DocumentRow>>($"api/documents/by-transport/{Id}");
            Documents = docs ?? new List<DocumentRow>();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (!CanUpload)
            return Forbid();

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            if (UploadFile == null || UploadFile.Length == 0)
            {
                Error = "Fișierul este obligatoriu.";
                await OnGetAsync();
                return Page();
            }

            if (Id == Guid.Empty)
            {
                Error = "Transport invalid.";
                await OnGetAsync();
                return Page();
            }

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(Id.ToString()), "TransportId");
            content.Add(new StringContent(Type.ToString()), "Type");

            await using var stream = UploadFile.OpenReadStream();
            var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(UploadFile.ContentType))
            {
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(UploadFile.ContentType);
            }

            content.Add(fileContent, "File", UploadFile.FileName);

            var resp = await client.PostAsync("api/documents/upload", content);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRegisterAsync()
    {
        if (!CanRegister)
            return Forbid();

        if (RegisterDocumentId == Guid.Empty)
        {
            Error = "Document invalid.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var resp = await client.PostAsync($"api/documents/{RegisterDocumentId}/register-onchain", null);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Register failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    // ---------- Local DTOs ----------
    public sealed class TransportRow
    {
        public Guid Id { get; set; }
        public string? ReferenceNo { get; set; }
    }

    public sealed class DocumentRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public string? OriginalFileName { get; set; }
        public string? Sha256 { get; set; }

        // blockchain
        public string? BlockchainTxId { get; set; }
        public string? ChainStatus { get; set; }
        public DateTime? RegisteredOnChainAtUtc { get; set; }
        public string? ChainError { get; set; }
    }
}