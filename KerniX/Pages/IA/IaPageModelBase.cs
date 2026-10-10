using KerniX.Models.IA;
using KerniX.Security;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.IA;

public abstract class IaPageModelBase : PageModel
{
    protected readonly IKerniXIaService Ia;
    protected readonly ILogger Log;
    protected IaPageModelBase(IKerniXIaService ia, ILogger log)
    {
        Ia = ia;
        Log = log;
    }

    public IaContexto? Contexto { get; private set; }
    public string MensajeError { get; protected set; } = "";

    protected async Task<IActionResult?> ValidarContextoAsync(CancellationToken ct)
    {
        // El contexto se obtiene del usuario autenticado; no se acepta idEmpresa por URL.
        int idUsuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        if (idUsuario <= 0)
            return RedirectToPage("/Account/Login");

        try
        {
            Contexto = await Ia.ObtenerContextoAsync(idUsuario, ct);
            if (Contexto is null)
                MensajeError = "No se pudo validar una empresa activa para esta sesión. Revisa la vinculación del usuario con su empleado.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.LogError(ex, "[KERNIX IA] Fallo validando contexto Usuario={Usuario}", idUsuario);
            MensajeError = "No fue posible validar el contexto de empresa. Revisa los registros del servidor.";
        }
        return null;
    }
}
