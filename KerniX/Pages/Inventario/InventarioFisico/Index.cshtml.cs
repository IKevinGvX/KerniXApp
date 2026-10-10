using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.InventarioFisico;

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
    public List<InventarioFisicoListaItem> Items { get; private set; } = new();

    public int EnConteo => Items.Count(x => EsEstado(x.Estado, "en_conteo"));
    public int Cerrados => Items.Count(x => EsEstado(x.Estado, "cerrado"));
    public int Aplicados => Items.Count(x => EsEstado(x.Estado, "aplicado"));
    public int Pendientes => Items.Sum(x => x.PendientesConteo);

    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync(int idAlmacen, string? observacion, CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[INV FISICO] POST Crear recibido. Empresa={Empresa} Empleado={Empleado} Almacen={Almacen}",
            IdEmpresa, IdEmpleado, idAlmacen
        );

        try
        {
            if (idAlmacen <= 0)
                throw new InvalidOperationException("Selecciona un almacén.");

            long id = await _service.IniciarInventarioFisicoAsync(
                IdEmpresa, idAlmacen, IdEmpleado, observacion, ct);

            return RedirectToPage("./Detalle", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INV FISICO] Error.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
        Items = await _service.ListarInventariosFisicosAsync(
            IdEmpresa,
            string.IsNullOrWhiteSpace(Estado) ? null : Estado,
            ct
        );
    }

    private static bool EsEstado(string? actual, string esperado) =>
        string.Equals(actual?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
