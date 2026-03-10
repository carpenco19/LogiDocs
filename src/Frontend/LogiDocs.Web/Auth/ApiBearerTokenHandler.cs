using System.Net.Http.Headers;

namespace LogiDocs.Web.Auth;

public sealed class ApiBearerTokenHandler : DelegatingHandler
{
    private readonly IApiTokenService _tokenService;

    public ApiBearerTokenHandler(IApiTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateAccessTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}