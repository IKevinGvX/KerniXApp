using KerniX.Models.Proveedores;
using KerniX.Services.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Proveedores;

public sealed class DetalleModel : PageModel
{
    private readonly IProveedorDbService _proveedores;
    private readonly ProveedorContextService _seguridad;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(IProveedorDbService proveedores,
        ProveedorContextService seguridad, ILogger<DetalleModel> logger)
    {
        _proveedores = proveedores;
        _seguridad = seguridad;
        _logger = logger;
    }

    public ProveedorItem Proveedor { get; private set; } = new();
    public bool PuedeGestionar { get; private set; }
    [TempData] public string? MensajeOk { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        if (id <= 0) return NotFound();
        PuedeGestionar = ctx.PuedeGestionar;
        try
        {
            var proveedor = await _proveedores.ObtenerAsync(ctx.IdEmpresa, id, cancellationToken);
            if (proveedor is null) return NotFound();
            Proveedor = proveedor;
            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error SQL detalle Id={Id}", id);
            return StatusCode(500);
        }
    }
}
