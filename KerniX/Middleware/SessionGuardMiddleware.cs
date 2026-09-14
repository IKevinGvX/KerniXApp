using KerniX.Security;

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

    public async Task InvokeAsync(HttpContext context)
    {
        var path =
            context.Request.Path.Value?.ToLowerInvariant()
            ?? string.Empty;

        // =========================================================
        // RUTAS QUE NO REQUIEREN LOGIN
        // =========================================================

        if (EsRutaPublica(path))
        {
            await _next(context);
            return;
        }

        // =========================================================
        // VALIDAR TOKEN
        // =========================================================

        var token =
            context.Session.GetString(
                SessionKeys.AccessToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation(
                "Acceso sin sesión a {Path}. Redirigiendo al login.",
                path);

            // NO Session.Clear() aquí.
            // Logout es quien se encarga de limpiar todo.

            context.Response.Redirect(
                "/Account/Login");

            return;
        }

        await _next(context);
    }

    private static bool EsRutaPublica(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return
            path == "/" ||

            path == "/login" ||
            path == "/account/login" ||
            path.StartsWith("/account/login/") ||

            path == "/account/logout" ||
            path.StartsWith("/account/logout/") ||

            path.StartsWith("/css/") ||
            path.StartsWith("/js/") ||
            path.StartsWith("/images/") ||
            path.StartsWith("/lib/") ||
            path.StartsWith("/fonts/") ||

            path.StartsWith("/favicon") ||

            path.EndsWith(".css") ||
            path.EndsWith(".js") ||
            path.EndsWith(".png") ||
            path.EndsWith(".jpg") ||
            path.EndsWith(".jpeg") ||
            path.EndsWith(".gif") ||
            path.EndsWith(".svg") ||
            path.EndsWith(".webp") ||
            path.EndsWith(".ico");
    }
}