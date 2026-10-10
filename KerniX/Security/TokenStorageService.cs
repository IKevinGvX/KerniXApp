using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace KerniX.Security;

public sealed class TokenStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenStorageService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public void GuardarToken(string accessToken)
    {
        if (Session is null || string.IsNullOrWhiteSpace(accessToken))
            return;

        Session.SetString(SessionKeys.AccessToken, accessToken);
    }

    public void GuardarRefreshToken(string refreshToken)
    {
        if (Session is null || string.IsNullOrWhiteSpace(refreshToken))
            return;

        Session.SetString(SessionKeys.RefreshToken, refreshToken);
    }

    public string? ObtenerToken() =>
        Session?.GetString(SessionKeys.AccessToken);

    public string? ObtenerRefreshToken() =>
        Session?.GetString(SessionKeys.RefreshToken);

    // Comprueba caducidad de JWT; NO sustituye la validación
    // criptográfica que debe realizar la API.
    public bool EstaAutenticado() =>
        TokenNoExpirado(ObtenerToken());

    private static bool TokenNoExpirado(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            string[] partes = token.Split('.');
            if (partes.Length != 3)
                return false;

            string payload = partes[1]
                .Replace('-', '+')
                .Replace('_', '/');

            int resto = payload.Length % 4;
            if (resto == 1)
                return false;
            if (resto > 0)
                payload = payload.PadRight(payload.Length + (4 - resto), '=');

            byte[] bytes = Convert.FromBase64String(payload);
            using JsonDocument documento = JsonDocument.Parse(bytes);

            if (!documento.RootElement.TryGetProperty("exp", out JsonElement exp)
                || exp.ValueKind != JsonValueKind.Number
                || !exp.TryGetInt64(out long fechaExpiracion))
            {
                return false;
            }

            return fechaExpiracion > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void EliminarTokens()
    {
        if (Session is null)
            return;

        Session.Remove(SessionKeys.AccessToken);
        Session.Remove(SessionKeys.RefreshToken);
    }

    public void LimpiarSesion()
    {
        Session?.Clear();
    }
}
