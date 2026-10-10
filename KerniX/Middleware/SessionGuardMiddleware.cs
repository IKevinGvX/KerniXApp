using System;
using System.Text.Json;
using System.Threading.Tasks;
using KerniX.Security;
using KerniX.Services.Auth;
using KerniX.Services.Sesiones;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KerniX.Middleware;

public sealed class SessionGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionGuardMiddleware> _logger;

    public SessionGuardMiddleware(
        RequestDelegate next,
        ILogger<SessionGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // DI por solicitud: no guardar servicios Scoped en el constructor.
    public async Task InvokeAsync(
        HttpContext context,
        IAuthApiService authApiService,
        ISesionWebDbService sesionWeb)
    {
        string path = context.Request.Path.Value?.ToLowerInvariant()
                      ?? string.Empty;

        if (EsRutaPublica(path))
        {
            await _next(context);
            return;
        }

        string? token = context.Session.GetString(SessionKeys.AccessToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation(
                "Acceso sin token a {Path}", path);
            Rechazar(context);
            return;
        }

        // Control remoto y por inactividad de la sesión web.
        // La validación de la firma del JWT sigue en la API.
        int idUsuario = context.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        string? sessionId = context.Session.GetString(SesionWebKeys.IdSesion);
        if (idUsuario <= 0 || !Guid.TryParse(sessionId, out Guid idSesionWeb))
        {
            context.Session.Clear();
            Rechazar(context);
            return;
        }

        try
        {
            if (!await sesionWeb.ValidarYActualizarAsync(
                    idSesionWeb, idUsuario, context.RequestAborted))
            {
                _logger.LogInformation(
                    "Sesión web vencida o revocada. Usuario={IdUsuario}", idUsuario);
                context.Session.Clear();
                Rechazar(context);
                return;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "No se pudo verificar la sesión web en SQL Server.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("La seguridad de sesiones no está disponible.");
            return;
        }

        if (!TryGetExpiracionJwt(token, out long expiracionUnix))
        {
            // La presencia de texto no significa autenticación válida.
            _logger.LogWarning("Token sin expiración JWT reconocible en {Path}", path);
            context.Session.Clear();
            Rechazar(context);
            return;
        }

        long ahoraUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Sólo renovar si caduca en 60s o ya caducó.
        if (expiracionUnix <= ahoraUnix + 60)
        {
            string? refresh = context.Session.GetString(SessionKeys.RefreshToken);

            if (!string.IsNullOrWhiteSpace(refresh))
            {
                try
                {
                    var resultado = await authApiService.RefreshAsync(
                        refresh,
                        context.RequestAborted);

                    string? nuevoToken = resultado?.Data?.AccessToken;

                    // Comprobación de formato/expiración, NO de firma.
                    if (resultado?.Success == true &&
                        TryGetExpiracionJwt(nuevoToken, out long nuevaExp) &&
                        nuevaExp > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5)
                    {
                        context.Session.SetString(
                            SessionKeys.AccessToken, nuevoToken!);

                        if (!string.IsNullOrWhiteSpace(resultado.Data?.RefreshToken))
                        {
                            context.Session.SetString(
                                SessionKeys.RefreshToken,
                                resultado.Data.RefreshToken);
                        }

                        await _next(context);
                        return;
                    }
                }
                catch (OperationCanceledException)
                    when (context.RequestAborted.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // No registrar nunca el refresh token en logs.
                    _logger.LogWarning(ex,
                        "La renovación JWT falló en {Path}", path);
                }
            }
        }

        // Permite terminar de usar un access token aún vigente,
        // incluso si el refresh no está disponible o no está configurado.
        if (expiracionUnix > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            await _next(context);
            return;
        }

        // Ha caducado y no se pudo renovar: limpiar valores caducados.
        context.Session.Clear();
        _logger.LogInformation("Sesión JWT caducada en {Path}", path);
        Rechazar(context);
    }

    private static void Rechazar(HttpContext context)
    {
        // Evita entregar HTML de Login con HTTP 200 a llamadas fetch.
        bool json = context.Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest" ||
                    context.Request.Headers.Accept.ToString()
                        .Contains("application/json", StringComparison.OrdinalIgnoreCase);

        context.Response.Headers["Cache-Control"] = "no-store";

        if (json)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.Redirect("/Account/Login");
    }

    // Lectura de exp para detectar vencimiento. NO valida firma,
    // issuer, audience ni revocación: eso corresponde a la API.
    private static bool TryGetExpiracionJwt(string? jwt, out long expUnix)
    {
        expUnix = 0;
        if (string.IsNullOrWhiteSpace(jwt)) return false;

        string[] partes = jwt.Split('.');
        if (partes.Length != 3 || partes[1].Length > 16384)
            return false;

        try
        {
            string payload = partes[1].Replace('-', '+').Replace('_', '/');
            int resto = payload.Length % 4;
            if (resto == 1) return false;
            if (resto != 0) payload = payload.PadRight(
                payload.Length + 4 - resto, '=');

            byte[] datos = Convert.FromBase64String(payload);
            using var json = JsonDocument.Parse(datos);
            return json.RootElement.TryGetProperty("exp", out var exp) &&
                   exp.ValueKind == JsonValueKind.Number &&
                   exp.TryGetInt64(out expUnix);
        }
        catch (FormatException) { return false; }
        catch (JsonException) { return false; }
        catch (ArgumentException) { return false; }
    }

    private static bool EsRutaPublica(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;

        return path == "/" ||
               path == "/login" ||
               path == "/account/login" ||
               path.StartsWith("/account/login/", StringComparison.Ordinal) ||
               path == "/account/forgotpassword" ||
               path == "/account/resetpassword" ||
               path == "/account/logout" ||
               path.StartsWith("/account/logout/", StringComparison.Ordinal) ||
               path.StartsWith("/css/", StringComparison.Ordinal) ||
               path.StartsWith("/js/", StringComparison.Ordinal) ||
               path.StartsWith("/images/", StringComparison.Ordinal) ||
               path.StartsWith("/lib/", StringComparison.Ordinal) ||
               path.StartsWith("/fonts/", StringComparison.Ordinal) ||
               path.StartsWith("/favicon", StringComparison.Ordinal) ||
               path == "/favicon.ico";
    }
}
