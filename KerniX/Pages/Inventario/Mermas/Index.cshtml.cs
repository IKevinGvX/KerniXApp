using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Mermas;

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
    public List<MermaItem> Items { get; private set; } = new();

    public decimal CantidadTotal => Items.Sum(x => x.Cantidad);
    public int Danos => Items.Count(x => EsCausa(x.Causa, "daño"));
    public int Vencimientos => Items.Count(x => EsCausa(x.Causa, "vencimiento"));

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
        decimal cantidad,
        string causa,
        string motivo,
        CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[MERMA] POST Registrar recibido. Empresa={Empresa} Empleado={Empleado} Almacen={Almacen} Producto={Producto} Cantidad={Cantidad} Causa={Causa}",
            IdEmpresa, IdEmpleado, idAlmacen, idProducto, cantidad, causa
        );

        try
        {
            if (idAlmacen <= 0 || idProducto <= 0)
                throw new InvalidOperationException("Almacén y producto son obligatorios.");

            if (cantidad <= 0)
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");

            if (string.IsNullOrWhiteSpace(causa) || string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Causa y motivo son obligatorios.");

            await _service.RegistrarMermaAsync(
                IdEmpresa,
                idAlmacen,
                idProducto,
                cantidad,
                causa,
                motivo,
                IdEmpleado,
                ct
            );

            TempData["MensajeExito"] = "Merma registrada y descontada del stock.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MERMA] Error.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
        Items = await _service.ListarMermasAsync(IdEmpresa, ct);
    }

    private static bool EsCausa(string? actual, string esperado) =>
        string.Equals(actual?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
