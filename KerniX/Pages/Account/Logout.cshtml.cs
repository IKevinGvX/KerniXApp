using KerniX.Security;
using KerniX.Services.Sesiones;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;
    private readonly ISesionWebDbService _sesiones;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(TokenStorageService tokenStorage,
        ISesionWebDbService sesiones, ILogger<LogoutModel> logger)
    {
        _tokenStorage = tokenStorage;
        _sesiones = sesiones;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        Response.Headers["Cache-Control"] = "no-store";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        int idUsuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        string? id = HttpContext.Session.GetString(SesionWebKeys.IdSesion);

        try
        {
            if (idUsuario > 0 && Guid.TryParse(id, out Guid idSesion))
            {
                await _sesiones.CerrarAsync(idSesion, idUsuario,
                    "logout", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo registrar el logout en SQL. Cerrando sesión local igualmente.");
        }
        finally
        {
            // SIEMPRE cerrar la sesión web, incluso si SQL falló.
            _tokenStorage.EliminarTokens();
            HttpContext.Session.Clear();
        }
        return RedirectToPage("/Account/Login");
    }
}
