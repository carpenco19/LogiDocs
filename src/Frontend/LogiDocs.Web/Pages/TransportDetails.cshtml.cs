using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

public class TransportDetailsModel : PageModel
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
    public List<DocumentRow>? Documents { get; set; }

    // ---------- Upload form (POST) ----------
    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public Guid TransportId { get; set; }

    [BindProperty]
    public int Type { get; set; }

    [BindProperty]
    public Guid UploadedByUserId { get; set; }

    // ---------- Register on chain (POST) ----------
    [BindProperty]
    public Guid RegisterDocumentId { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            // 1) Transport reference (quick MVP)
            var transports = await client.GetFromJsonAsync<List<TransportRow>>("api/Transports");
            var t = transports?.FirstOrDefault(x => x.Id == Id);
            TransportRef = t?.ReferenceNo ?? "Unknown";

            // 2) Documents for transport
            Documents = await client.GetFromJsonAsync<List<DocumentRow>>($"api/Documents/by-transport/{Id}");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ---- Register document on chain ----
        if (RegisterDocumentId != Guid.Empty)
        {
            try
            {
                var client = _factory.CreateClient("LogiDocsApi");

                var resp = await client.PostAsync($"api/Documents/{RegisterDocumentId}/register-onchain", null);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Blockchain error: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
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

        // ---- Upload document ----
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            // fallback if hidden TransportId is missing
            if (TransportId == Guid.Empty)
                TransportId = Id;

            if (UploadFile == null || UploadFile.Length == 0)
            {
                Error = "File is required.";
                await OnGetAsync();
                return Page();
            }

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(TransportId.ToString()), "TransportId");
            content.Add(new StringContent(Type.ToString()), "Type");
            content.Add(new StringContent(UploadedByUserId.ToString()), "UploadedByUserId");

            await using var stream = UploadFile.OpenReadStream();
            var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(UploadFile.ContentType))
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(UploadFile.ContentType);

            content.Add(fileContent, "File", UploadFile.FileName);

            var resp = await client.PostAsync("api/Documents/upload", content);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { id = TransportId });
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