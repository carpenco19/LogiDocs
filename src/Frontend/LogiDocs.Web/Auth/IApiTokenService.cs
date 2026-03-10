namespace LogiDocs.Web.Auth;

public interface IApiTokenService
{
    Task<string?> CreateAccessTokenAsync(CancellationToken ct = default);
}