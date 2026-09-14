using Microsoft.AspNetCore.Http;

namespace KerniX.Security;

public sealed class TokenStorageService
{
    private readonly IHttpContextAccessor
        _httpContextAccessor;


    public TokenStorageService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }


    // =========================================================
    // SESSION
    // =========================================================

    private ISession? Session =>
        _httpContextAccessor
            .HttpContext?
            .Session;


    // =========================================================
    // GUARDAR ACCESS TOKEN
    // =========================================================

    public void GuardarToken(
        string accessToken)
    {
        if (Session is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            return;
        }

        Session.SetString(
            SessionKeys.AccessToken,
            accessToken);
    }


    // =========================================================
    // GUARDAR REFRESH TOKEN
    // =========================================================

    public void GuardarRefreshToken(
        string refreshToken)
    {
        if (Session is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
            refreshToken))
        {
            return;
        }

        Session.SetString(
            SessionKeys.RefreshToken,
            refreshToken);
    }


    // =========================================================
    // OBTENER ACCESS TOKEN
    // =========================================================

    public string? ObtenerToken()
    {
        return Session?.GetString(
            SessionKeys.AccessToken);
    }


    // =========================================================
    // OBTENER REFRESH TOKEN
    // =========================================================

    public string? ObtenerRefreshToken()
    {
        return Session?.GetString(
            SessionKeys.RefreshToken);
    }


    // =========================================================
    // VALIDAR AUTENTICACIÓN
    // =========================================================

    public bool EstaAutenticado()
    {
        string? accessToken =
            ObtenerToken();

        return !string.IsNullOrWhiteSpace(
            accessToken);
    }


    // =========================================================
    // ELIMINAR TOKENS
    // =========================================================

    public void EliminarTokens()
    {
        if (Session is null)
        {
            return;
        }

        Session.Remove(
            SessionKeys.AccessToken);

        Session.Remove(
            SessionKeys.RefreshToken);
    }


    // =========================================================
    // CERRAR SESIÓN COMPLETA
    // =========================================================

    public void LimpiarSesion()
    {
        if (Session is null)
        {
            return;
        }

        Session.Clear();
    }
}