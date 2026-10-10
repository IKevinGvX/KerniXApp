using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.Inventario.Kardex;

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
    public KardexFiltro Filtro { get; set; } = new();

    public InventarioEnterpriseCatalogos Catalogos { get; private set; } = new();
    public KardexResultado Resultado { get; private set; } = new();

    public decimal Ingresos => Resultado.Items.Where(x => EsNaturaleza(x.Naturaleza, "ingreso")).Sum(x => x.Cantidad);
    public decimal Egresos => Resultado.Items.Where(x => EsNaturaleza(x.Naturaleza, "egreso")).Sum(x => x.Cantidad);
    public int Revertidos => Resultado.Items.Count(x => x.EstaRevertido);

    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!CargarSesion(requiereEmpleado: false)) return IrLogin();

        Filtro ??= new KardexFiltro();
        Filtro.IdEmpresa = IdEmpresa;

        if (!Filtro.IdSucursal.HasValue)
            Filtro.IdSucursal = IdSucursal;

        if (Filtro.Pagina <= 0)
            Filtro.Pagina = 1;

        if (Filtro.TamanoPagina <= 0)
            Filtro.TamanoPagina = 50;

        try
        {
            Catalogos = await _service.ObtenerCatalogosAsync(IdEmpresa, IdSucursal, ct);
            Resultado = await _service.ListarKardexAsync(Filtro, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KARDEX] Error.");
            MensajeError = ex.Message;
        }

        return Page();
    }

    private static bool EsNaturaleza(string? actual, string esperado) =>
        string.Equals(actual?.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
