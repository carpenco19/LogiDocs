using LogiDocs.Contracts.Transports;
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

    public IReadOnlyList<TransportModeOption> ModeOptions { get; } = new List<TransportModeOption>
    {
        new(1, "Road"),
        new(2, "Rail"),
        new(3, "Sea"),
        new(4, "Air")
    };

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Reference number is required.")]
        [StringLength(50, ErrorMessage = "Reference number is too long.")]
        public string ReferenceNo { get; set; } = "";

        public List<SegmentInputModel> Segments { get; set; } = new()
        {
            new SegmentInputModel()
        };
    }

    public sealed class SegmentInputModel
    {
        [Range(1, 4, ErrorMessage = "Please select a transport mode.")]
        public int Mode { get; set; }

        [Required(ErrorMessage = "Segment origin is required.")]
        [StringLength(100, ErrorMessage = "Segment origin is too long.")]
        public string Origin { get; set; } = "";

        [Required(ErrorMessage = "Segment destination is required.")]
        [StringLength(100, ErrorMessage = "Segment destination is too long.")]
        public string Destination { get; set; } = "";

        [StringLength(100, ErrorMessage = "Operator name is too long.")]
        public string? OperatorName { get; set; }
    }

    public readonly record struct TransportModeOption(int Value, string Text);

    public void OnGet()
    {
        EnsureAtLeastOneSegment();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureAtLeastOneSegment();
        NormalizeSegments();

        ValidateSegments();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var client = _factory.CreateClient("LogiDocsApi");

            var firstSegment = Input.Segments.First();
            var lastSegment = Input.Segments.Last();

            var req = new CreateTransportRequest
            {
                ReferenceNo = Input.ReferenceNo.Trim(),
                Origin = firstSegment.Origin.Trim(),
                Destination = lastSegment.Destination.Trim(),
                Segments = Input.Segments
                    .Select((s, index) => new CreateTransportSegmentRequest
                    {
                        OrderNo = index + 1,
                        Mode = s.Mode,
                        Origin = s.Origin.Trim(),
                        Destination = s.Destination.Trim(),
                        OperatorName = string.IsNullOrWhiteSpace(s.OperatorName)
                            ? null
                            : s.OperatorName.Trim()
                    })
                    .ToList()
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

    private void EnsureAtLeastOneSegment()
    {
        Input ??= new InputModel();

        if (Input.Segments == null || Input.Segments.Count == 0)
        {
            Input.Segments = new List<SegmentInputModel>
            {
                new SegmentInputModel()
            };
        }
    }

    private void NormalizeSegments()
    {
        Input.Segments = Input.Segments
            .Where(s =>
                s != null &&
                (!string.IsNullOrWhiteSpace(s.Origin) ||
                 !string.IsNullOrWhiteSpace(s.Destination) ||
                 !string.IsNullOrWhiteSpace(s.OperatorName) ||
                 s.Mode != 0))
            .ToList();

        if (Input.Segments.Count == 0)
        {
            Input.Segments.Add(new SegmentInputModel());
        }
    }

    private void ValidateSegments()
    {
        if (Input.Segments.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one segment is required.");
            return;
        }

        for (int i = 0; i < Input.Segments.Count; i++)
        {
            var segment = Input.Segments[i];

            if (segment.Mode < 1 || segment.Mode > 4)
            {
                ModelState.AddModelError($"Input.Segments[{i}].Mode", "Please select a transport mode.");
            }

            if (string.IsNullOrWhiteSpace(segment.Origin))
            {
                ModelState.AddModelError($"Input.Segments[{i}].Origin", "Segment origin is required.");
            }

            if (string.IsNullOrWhiteSpace(segment.Destination))
            {
                ModelState.AddModelError($"Input.Segments[{i}].Destination", "Segment destination is required.");
            }
        }
    }
}