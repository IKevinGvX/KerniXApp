using KerniX.Models.Proveedores;
using KerniX.Services.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Proveedores;

public sealed class HistorialModel : PageModel
{
    private readonly IProveedorDbService _proveedores;
    private readonly ProveedorContextService _seguridad;
    private readonly ILogger<HistorialModel> _logger;

    public HistorialModel(IProveedorDbService proveedores,
        ProveedorContextService seguridad, ILogger<HistorialModel> logger)
    {
        _proveedores = proveedores;
        _seguridad = seguridad;
        _logger = logger;
    }

    public ProveedorItem Proveedor { get; private set; } = new();
    public IReadOnlyList<ProveedorEvento> Eventos { get; private set; } = Array.Empty<ProveedorEvento>();
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        if (id <= 0) return NotFound();
        try
        {
            var proveedor = await _proveedores.ObtenerAsync(ctx.IdEmpresa, id, cancellationToken);
            if (proveedor is null) return NotFound();
            Proveedor = proveedor;
            Eventos = await _proveedores.HistorialAsync(ctx.IdEmpresa, id, cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error SQL historial Id={Id}", id);
            Error = "No se pudo leer la auditoría. Comprueba la instalación de la tabla y los permisos SQL.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Estructura incompatible Id={Id}", id);
            Error = "No se pudo consultar el historial con la estructura SQL actual.";
        }
        return Page();
    }
}
