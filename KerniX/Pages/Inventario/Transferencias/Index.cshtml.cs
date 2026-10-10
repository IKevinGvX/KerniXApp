using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Transferencias;

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
    public string? Estado { get; set; }

    public InventarioEnterpriseCatalogos Catalogos { get; private set; } = new();
    public List<TransferenciaListaItem> Items { get; private set; } = new();

    public int Borradores => Items.Count(x => EsEstado(x.Estado, "borrador"));
    public int Aprobadas => Items.Count(x => EsEstado(x.Estado, "aprobada"));
    public int Despachadas => Items.Count(x => EsEstado(x.Estado, "despachada"));
    public int Recibidas => Items.Count(x => EsEstado(x.Estado, "recibida"));

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync(
        int idAlmacenOrigen,
        int idAlmacenDestino,
        int idProducto,
        decimal cantidad,
        string? observacion,
        CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[TRANSFERENCIA] POST Crear recibido. Empresa={Empresa} Empleado={Empleado} Origen={Origen} Destino={Destino} Producto={Producto} Cantidad={Cantidad}",
            IdEmpresa, IdEmpleado, idAlmacenOrigen, idAlmacenDestino, idProducto, cantidad
        );

        try
        {
            if (idAlmacenOrigen <= 0 || idAlmacenDestino <= 0)
                throw new InvalidOperationException("Selecciona almacén origen y destino.");

            if (idAlmacenOrigen == idAlmacenDestino)
                throw new InvalidOperationException("Origen y destino deben ser diferentes.");

            if (idProducto <= 0)
                throw new InvalidOperationException("Selecciona un producto.");

            if (cantidad <= 0)
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");

            long id = await _service.CrearTransferenciaAsync(
                IdEmpresa,
                idAlmacenOrigen,
                idAlmacenDestino,
                IdEmpleado,
                observacion,
                ct
            );

            await _service.AgregarDetalleTransferenciaAsync(
                id,
                idProducto,
                cantidad,
                ct
            );

            TempData["MensajeExito"] = $"Transferencia #{id} creada.";
            return RedirectToPage("./Detalle", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TRANSFERENCIA] Error creando.");
            MensajeError = ex is SqlException sql
                ? $"SQL {sql.Number}: {sql.Message}"
                : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(
            IdEmpresa,
            IdSucursal,
            ct
        );

        Items = await _service.ListarTransferenciasAsync(
            IdEmpresa,
            string.IsNullOrWhiteSpace(Estado) ? null : Estado,
            ct
        );
    }

    private static bool EsEstado(string? actual, string esperado) =>
        string.Equals(actual?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
