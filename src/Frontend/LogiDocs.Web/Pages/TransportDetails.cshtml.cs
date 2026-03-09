using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;
using LogiDocs.Contracts.Documents;

namespace LogiDocs.Web.Pages;

[Authorize]
public sealed class TransportDetailsModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public TransportDetailsModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string? Error { get; set; }
    public string? TransportRef { get; set; }
    public List<DocumentRow> Documents { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public int Type { get; set; }

    [BindProperty]
    public Guid RegisterDocumentId { get; set; }

    [BindProperty]
    public Guid VerifyDocumentId { get; set; }

    [BindProperty]
    public Guid ValidateDocumentId { get; set; }

    [BindProperty]
    public Guid RejectDocumentId { get; set; }

    public DocumentVerificationDto? VerificationResult { get; set; }

    public bool CanUpload =>
        User.IsInRole("Shipper") ||
        User.IsInRole("Carrier") ||
        User.IsInRole("Administrator");

    public bool CanRegister =>
        User.IsInRole("CustomsBroker") ||
        User.IsInRole("Administrator");

    public bool CanVerify =>
        User.IsInRole("CustomsAuthority") ||
        User.IsInRole("Administrator");

    public bool CanValidate =>
        User.IsInRole("CustomsAuthority") ||
        User.IsInRole("Administrator");

    public async Task OnGetAsync()
    {
        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var transports = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");
            var transport = transports?.FirstOrDefault(x => x.Id == Id);

            TransportRef = transport?.ReferenceNo ?? "Unknown";

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

        if (Id == Guid.Empty)
        {
            Error = "Invalid transport.";
            await OnGetAsync();
            return Page();
        }

        if (UploadFile == null || UploadFile.Length == 0)
        {
            Error = "The file is required.";
            await OnGetAsync();
            return Page();
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            Error = "You are not authenticated. Please log in.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(Id.ToString()), "TransportId");
            content.Add(new StringContent(Type.ToString()), "Type");
            content.Add(new StringContent(userId.ToString()), "UploadedByUserId");

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

            SuccessMessage = "Document uploaded successfully.";
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
            Error = "Invalid document.";
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
                Error = $"Registration failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = "Document registered on blockchain.";
            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostVerifyAsync()
    {
        if (!CanVerify)
            return Forbid();

        if (VerifyDocumentId == Guid.Empty)
        {
            Error = "Invalid document.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var result = await client.GetFromJsonAsync<DocumentVerificationDto>(
                $"api/documents/{VerifyDocumentId}/verify");

            if (result == null)
            {
                Error = "The verification result could not be obtained.";
                await OnGetAsync();
                return Page();
            }

            VerificationResult = result;

            var transports = await client.GetFromJsonAsync<List<TransportRow>>("api/transports");
            var transport = transports?.FirstOrDefault(x => x.Id == Id);
            TransportRef = transport?.ReferenceNo ?? "Unknown";

            var docs = await client.GetFromJsonAsync<List<DocumentRow>>($"api/documents/by-transport/{Id}");
            Documents = docs ?? new List<DocumentRow>();

            SuccessMessage = "Document verification completed.";
            return Page();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        if (!CanValidate)
            return Forbid();

        if (ValidateDocumentId == Guid.Empty)
        {
            Error = "Invalid document.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var resp = await client.PostAsync($"api/documents/{ValidateDocumentId}/validate", null);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Validation failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = "Document validated successfully.";
            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        if (!CanValidate)
            return Forbid();

        if (RejectDocumentId == Guid.Empty)
        {
            Error = "Invalid document.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var resp = await client.PostAsync($"api/documents/{RejectDocumentId}/reject", null);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Rejection failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = "Document rejected successfully.";
            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }

    public sealed class TransportRow
    {
        public Guid Id { get; set; }
        public string? ReferenceNo { get; set; }
    }

    public sealed class DocumentRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }
        public string? OriginalFileName { get; set; }
        public string? Sha256 { get; set; }
        public string? BlockchainTxId { get; set; }
        public string? ChainStatus { get; set; }
        public DateTime? RegisteredOnChainAtUtc { get; set; }
        public string? ChainError { get; set; }

        public string TypeName => Type switch
        {
            0 => "CMR",
            1 => "Invoice",
            2 => "Packing List",
            _ => "Other"
        };

        public string StatusName => Status switch
        {
            0 => "Uploaded",
            1 => "Verified",
            2 => "Tampered",
            3 => "Rejected",
            _ => "Unknown"
        };
    }
}