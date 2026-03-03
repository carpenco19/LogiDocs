using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using LogiDocs.Contracts.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogiDocs.Web.Pages;

[Authorize]
public class CreateTransportModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public CreateTransportModel(IHttpClientFactory http)
    {
        _http = http;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required]
        public string ReferenceNo { get; set; } = "";

        [Required]
        public string Origin { get; set; } = "";

        [Required]
        public string Destination { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var client = _http.CreateClient("LogiDocsApi");

            var req = new CreateTransportRequest
            {
                ReferenceNo = Input.ReferenceNo,
                Origin = Input.Origin,
                Destination = Input.Destination
            };

            var resp = await client.PostAsJsonAsync("api/transports", req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                Error = $"API error: {(int)resp.StatusCode} {resp.ReasonPhrase}";
                return Page();
            }

            return RedirectToPage("/Transports");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}