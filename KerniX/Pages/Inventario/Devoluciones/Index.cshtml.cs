using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Devoluciones;

public sealed class IndexModel : InventarioEnterprisePageModel
{
    private readonly IInventarioEnterpriseDbService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IInventarioEnterpriseDbService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? TipoFiltro { get; set; }

    public InventarioEnterpriseCatalogos Catalogos { get; private set; } = new();
    public List<DevolucionInventarioItem> Items { get; private set; } = new();

    public int Clientes => Items.Count(x => EsTipo(x.Tipo, "cliente"));
    public int Proveedores => Items.Count(x => EsTipo(x.Tipo, "proveedor"));
    public decimal CantidadTotal => Items.Sum(x => x.Cantidad);

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRegistrarAsync(
        string tipo,
        int idAlmacen,
        int idProducto,
        decimal cantidad,
        string? origenTipo,
        long? origenId,
        string? documentoOrigen,
        string? terceroDocumento,
        string? terceroNombre,
        string motivo,
        CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[DEVOLUCION] POST Registrar recibido. Tipo={Tipo} Empresa={Empresa} Empleado={Empleado} Almacen={Almacen} Producto={Producto} Cantidad={Cantidad}",
            tipo, IdEmpresa, IdEmpleado, idAlmacen, idProducto, cantidad
        );

        try
        {
            string t = (tipo ?? "").Trim().ToLowerInvariant();

            if (t != "cliente" && t != "proveedor")
                throw new InvalidOperationException("Tipo de devolución inválido.");

            if (idAlmacen <= 0 || idProducto <= 0)
                throw new InvalidOperationException("Almacén y producto son obligatorios.");

            if (cantidad <= 0)
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("El motivo es obligatorio.");

            await _service.RegistrarDevolucionAsync(
                t,
                IdEmpresa,
                idAlmacen,
                idProducto,
                cantidad,
                origenTipo,
                origenId,
                documentoOrigen,
                terceroDocumento,
                terceroNombre,
                motivo,
                IdEmpleado,
                ct
            );

            TempData["MensajeExito"] =
                t == "cliente"
                    ? "Devolución de cliente registrada: ingreso de stock."
                    : "Devolución a proveedor registrada: egreso de stock.";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEVOLUCION] Error.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
        Items = await _service.ListarDevolucionesAsync(
            IdEmpresa,
            string.IsNullOrWhiteSpace(TipoFiltro) ? null : TipoFiltro,
            ct
        );
    }

    private static bool EsTipo(string? actual, string esperado) =>
        string.Equals(actual?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
