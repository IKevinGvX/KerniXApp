using System.Net.Http.Headers;
using KerniX.Security;

namespace KerniX.Services.Auth;

public sealed class AuthHeaderHandler
    : DelegatingHandler
{
    private readonly TokenStorageService
        _tokenStorage;

    public AuthHeaderHandler(
        TokenStorageService tokenStorage)
    {
        _tokenStorage =
            tokenStorage;
    }

    protected override async Task<HttpResponseMessage>
        SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        string? token =
            _tokenStorage.ObtenerToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}