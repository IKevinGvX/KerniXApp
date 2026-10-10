using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Ajustes;

public sealed class IndexModel : InventarioEnterprisePageModel
{
    private readonly IInventarioEnterpriseDbService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IInventarioEnterpriseDbService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public InventarioEnterpriseCatalogos Catalogos { get; private set; } = new();
    public List<AjusteInventarioItem> Items { get; private set; } = new();

    public int Positivos => Items.Count(x => x.Diferencia > 0);
    public int Negativos => Items.Count(x => x.Diferencia < 0);
    public decimal Neto => Items.Sum(x => x.Diferencia);

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
        int idAlmacen,
        int idProducto,
        decimal diferencia,
        string motivo,
        CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[AJUSTE] POST Registrar recibido. Empresa={Empresa} Empleado={Empleado} Almacen={Almacen} Producto={Producto} Diferencia={Diferencia}",
            IdEmpresa, IdEmpleado, idAlmacen, idProducto, diferencia
        );

        try
        {
            if (idAlmacen <= 0 || idProducto <= 0)
                throw new InvalidOperationException("Almacén y producto son obligatorios.");

            if (diferencia == 0)
                throw new InvalidOperationException("La diferencia no puede ser cero.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("El motivo es obligatorio.");

            await _service.RegistrarAjusteAsync(
                IdEmpresa,
                idAlmacen,
                idProducto,
                diferencia,
                IdEmpleado,
                motivo,
                ct
            );

            TempData["MensajeExito"] =
                diferencia > 0
                    ? "Ajuste positivo registrado."
                    : "Ajuste negativo registrado.";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AJUSTE] Error.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
        Items = await _service.ListarAjustesAsync(IdEmpresa, ct);
    }
}
