using LogiDocs.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;

namespace LogiDocs.Web.Pages;

[Authorize(Roles = $"{Roles.Shipper},{Roles.Administrator}")]
public sealed class CreateTransportModel : PageModel
{
    private readonly IHttpClientFactory _factory;

    public CreateTransportModel(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "ReferenceNo is required.")]
        [StringLength(50, ErrorMessage = "ReferenceNo is too long.")]
        public string ReferenceNo { get; set; } = "";

        [Required(ErrorMessage = "Origin is required.")]
        [StringLength(100, ErrorMessage = "Origin is too long.")]
        public string Origin { get; set; } = "";

        [Required(ErrorMessage = "Destination is required.")]
        [StringLength(100, ErrorMessage = "Destination is too long.")]
        public string Destination { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var req = new CreateTransportRequest
            {
                ReferenceNo = Input.ReferenceNo.Trim(),
                Origin = Input.Origin.Trim(),
                Destination = Input.Destination.Trim()
            };

            var resp = await client.PostAsJsonAsync("api/transports", req);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Create failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                return Page();
            }

            var raw = await resp.Content.ReadAsStringAsync();
            var idText = raw.Trim().Trim('"');

            if (!Guid.TryParse(idText, out var id))
            {
                Error = $"Create failed: invalid id returned: {raw}";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return RedirectToPage("/TransportDetails", new { id });
        }
        catch (HttpRequestException ex)
        {
            Error = $"HTTP error: {ex.Message}";
            return Page();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    public sealed class CreateTransportRequest
    {
        public string ReferenceNo { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
    }
}