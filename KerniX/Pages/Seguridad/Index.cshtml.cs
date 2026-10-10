using KerniX.Security;
using KerniX.Services.Sesiones;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Seguridad.Sesiones;

public sealed class IndexModel : PageModel
{
    private readonly ISesionWebDbService _sesiones;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ISesionWebDbService sesiones, ILogger<IndexModel> logger)
    {
        _sesiones = sesiones;
        _logger = logger;
    }

    public IReadOnlyList<SesionWebResumen> Sesiones { get; private set; } =
        Array.Empty<SesionWebResumen>();

    public Guid IdActual { get; private set; }

    [TempData] public string? Mensaje { get; set; }
    [TempData] public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        if (!ObtenerIdentidad(out int idUsuario, out Guid idActual))
            return RedirectToPage("/Account/Login");

        IdActual = idActual;
        try
        {
            Sesiones = await _sesiones.ListarActivasAsync(
                idUsuario, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando sesiones del usuario {IdUsuario}", idUsuario);
            Error = "No fue posible consultar las sesiones. Inténtalo más tarde.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostCerrarAsync(
        Guid id, CancellationToken cancellationToken)
    {
        if (!ObtenerIdentidad(out int idUsuario, out Guid idActual))
            return RedirectToPage("/Account/Login");

        if (id == Guid.Empty || id == idActual)
        {
            Error = "Para salir de este dispositivo utiliza Cerrar sesión.";
            return RedirectToPage();
        }

        try
        {
            bool cerrada = await _sesiones.CerrarAsync(
                id, idUsuario, "cierre_remoto", cancellationToken);
            Mensaje = cerrada
                ? "Se cerró la sesión del dispositivo seleccionado."
                : "La sesión ya no está activa o no pertenece a tu usuario.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de revocación remota para usuario {IdUsuario}", idUsuario);
            Error = "No fue posible cerrar la sesión seleccionada.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCerrarOtrasAsync(
        CancellationToken cancellationToken)
    {
        if (!ObtenerIdentidad(out int idUsuario, out Guid idActual))
            return RedirectToPage("/Account/Login");

        try
        {
            int cerradas = await _sesiones.CerrarOtrasAsync(
                idActual, idUsuario, cancellationToken);
            Mensaje = $"Sesiones cerradas en otros dispositivos: {cerradas}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cerrando otras sesiones de {IdUsuario}", idUsuario);
            Error = "No fue posible cerrar las otras sesiones.";
        }
        return RedirectToPage();
    }

    private bool ObtenerIdentidad(out int idUsuario, out Guid idActual)
    {
        idUsuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        string? id = HttpContext.Session.GetString(SesionWebKeys.IdSesion);
        bool valido = Guid.TryParse(id, out idActual);
        return idUsuario > 0 && valido && idActual != Guid.Empty;
    }
}
