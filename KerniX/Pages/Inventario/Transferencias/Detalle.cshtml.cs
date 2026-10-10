using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Transferencias;

public sealed class DetalleModel : InventarioEnterprisePageModel
{
    private readonly IInventarioEnterpriseDbService _service;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(IInventarioEnterpriseDbService service, ILogger<DetalleModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public TransferenciaDetalleResultado? Documento { get; private set; }
    public InventarioEnterpriseCatalogos Catalogos { get; private set; } = new();
    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(id, ct);
        return Documento is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAgregarDetalleAsync(long id, int idProducto, decimal cantidad, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[TRANSFERENCIA] POST AgregarDetalle recibido. Transferencia={Transferencia} Producto={Producto} Cantidad={Cantidad}",
            id, idProducto, cantidad
        );

        try
        {
            if (idProducto <= 0 || cantidad <= 0)
                throw new InvalidOperationException("Producto y cantidad son obligatorios.");

            await _service.AgregarDetalleTransferenciaAsync(id, idProducto, cantidad, ct);
            TempData["MensajeExito"] = "Detalle actualizado.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TRANSFERENCIA] Error detalle.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(id, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAccionAsync(long id, string accion, string? motivo, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[TRANSFERENCIA] POST Accion recibido. Transferencia={Transferencia} Accion={Accion} Empleado={Empleado}",
            id, accion, IdEmpleado
        );

        try
        {
            switch ((accion ?? "").Trim().ToLowerInvariant())
            {
                case "aprobar":
                    await _service.AprobarTransferenciaAsync(id, IdEmpleado, ct);
                    break;

                case "despachar":
                    await _service.DespacharTransferenciaAsync(id, IdEmpleado, ct);
                    break;

                case "recibir":
                    await _service.RecibirTransferenciaAsync(id, IdEmpleado, ct);
                    break;

                case "cancelar":
                    if (string.IsNullOrWhiteSpace(motivo))
                        throw new InvalidOperationException("El motivo de cancelación es obligatorio.");

                    await _service.CancelarTransferenciaAsync(id, IdEmpleado, motivo, ct);
                    break;

                default:
                    throw new InvalidOperationException("Acción inválida.");
            }

            TempData["MensajeExito"] = "Acción completada.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TRANSFERENCIA] Error acción.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(id, ct);
        return Page();
    }

    private async Task CargarAsync(long id, CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
        Documento = await _service.ObtenerTransferenciaAsync(id, IdEmpresa, ct);
    }
}
