using KerniX.Models.Proveedores;
using KerniX.Services.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Proveedores;

public sealed class IndexModel : PageModel
{
    private readonly IProveedorDbService _proveedores;
    private readonly ProveedorContextService _seguridad;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IProveedorDbService proveedores,
        ProveedorContextService seguridad, ILogger<IndexModel> logger)
    {
        _proveedores = proveedores;
        _seguridad = seguridad;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "todos";
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;
    [TempData] public string? MensajeOk { get; set; }
    [TempData] public string? MensajeError { get; set; }
    public ProveedorListado Listado { get; private set; } = new();
    public string? Error { get; private set; }
    public bool PuedeGestionar { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var contexto = await _seguridad.ObtenerAsync(cancellationToken);
            if (contexto is null) return RedirectToPage("/Account/Login");
            PuedeGestionar = contexto.PuedeGestionar;
            Estado = (Estado ?? "todos").Trim().ToLowerInvariant();
            if (Estado is not ("todos" or "activos" or "inactivos")) Estado = "todos";
            bool? activo = Estado switch
            {
                "activos" => true,
                "inactivos" => false,
                _ => null
            };
            Listado = await _proveedores.ListarAsync(contexto.IdEmpresa,
                Buscar, activo, Pagina, 12, cancellationToken);
            Pagina = Listado.Pagina;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error SQL al listar.");
            Error = "Error de base de datos al listar proveedores. Revisa Salida en Visual Studio.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Falta configuración de base de datos.");
            Error = "No fue posible consultar proveedores. Revisa la configuración SQL del módulo.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostEstadoAsync(
        int idProveedor, bool activar, string? buscar, string? estado, int pagina,
        CancellationToken cancellationToken)
    {
        var contexto = await _seguridad.ObtenerAsync(cancellationToken);
        if (contexto is null) return RedirectToPage("/Account/Login");
        if (!contexto.PuedeGestionar) return StatusCode(403);
        if (idProveedor <= 0) return BadRequest();
        _logger.LogInformation("[PROVEEDORES] Estado POST recibido. Id={Id}, Activar={Activar}, Empresa={Empresa}",
            idProveedor, activar, contexto.IdEmpresa);
        try
        {
            bool ok = await _proveedores.CambiarEstadoAsync(
                contexto.IdEmpresa, contexto.IdUsuario, idProveedor, activar, cancellationToken);
            if (ok) MensajeOk = activar ? "Proveedor activado correctamente." : "Proveedor desactivado correctamente.";
            else MensajeError = "El proveedor no existe en tu empresa o no se pudo actualizar.";
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Falló cambio de estado Id={Id}", idProveedor);
            MensajeError = ex.Number == 208
                ? "Falta una tabla requerida. Ejecuta SQL/01_PROVEEDOR_AUDITORIA.sql en kernixerp."
                : "No se pudo actualizar el estado. Verifica si el proveedor tiene restricciones por compras.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[PROVEEDORES] Estado rechazado Id={Id}", idProveedor);
            MensajeError = "La estructura SQL no permite cambiar el estado del proveedor.";
        }
        return RedirectToPage("/Inventario/Proveedores/Index", new
        {
            Buscar = buscar,
            Estado = estado,
            Pagina = Math.Max(1, pagina)
        });
    }

    public string UrlPagina(int numero) =>
        Url.Page("/Inventario/Proveedores/Index", new { Buscar, Estado, Pagina = numero })
        ?? "/Inventario/Proveedores";
}
