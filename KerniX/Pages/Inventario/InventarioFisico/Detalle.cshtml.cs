using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.InventarioFisico;

public sealed class DetalleModel : InventarioEnterprisePageModel
{
    private readonly IInventarioEnterpriseDbService _service;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(IInventarioEnterpriseDbService service, ILogger<DetalleModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public InventarioFisicoDetalleResultado? Documento { get; private set; }
    public int Pendientes => Documento?.Detalles.Count(x => !x.CantidadContada.HasValue) ?? 0;
    public int Diferencias => Documento?.Detalles.Count(x => Math.Abs(x.Diferencia ?? 0M) > 0.0001M) ?? 0;
    public decimal DiferenciaNeta => Documento?.Detalles.Sum(x => x.Diferencia ?? 0M) ?? 0M;
    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(id, ct);
        return Documento is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostConteoAsync(long id, int idProducto, decimal cantidadContada, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[INV FISICO] POST Conteo recibido. Inventario={Inventario} Producto={Producto} Cantidad={Cantidad}",
            id, idProducto, cantidadContada
        );

        try
        {
            if (cantidadContada < 0)
                throw new InvalidOperationException("La cantidad no puede ser negativa.");

            await _service.GuardarConteoFisicoAsync(id, idProducto, cantidadContada, ct);
            TempData["MensajeExito"] = "Conteo guardado.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INV FISICO] Error conteo.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(id, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAccionAsync(long id, string accion, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[INV FISICO] POST Accion recibido. Inventario={Inventario} Accion={Accion} Empleado={Empleado}",
            id, accion, IdEmpleado
        );

        try
        {
            if (string.Equals(accion, "cerrar", StringComparison.OrdinalIgnoreCase))
            {
                await _service.CerrarInventarioFisicoAsync(id, IdEmpleado, ct);
                TempData["MensajeExito"] = "Conteo cerrado.";
            }
            else if (string.Equals(accion, "aplicar", StringComparison.OrdinalIgnoreCase))
            {
                await _service.AplicarInventarioFisicoAsync(id, IdEmpleado, ct);
                TempData["MensajeExito"] = "Diferencias aplicadas mediante movimientos de ajuste.";
            }
            else
            {
                throw new InvalidOperationException("Acción inválida.");
            }

            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INV FISICO] Error acción.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(id, ct);
        return Page();
    }

    private async Task CargarAsync(long id, CancellationToken ct)
    {
        Documento = await _service.ObtenerInventarioFisicoAsync(id, IdEmpresa, ct);
    }
}
