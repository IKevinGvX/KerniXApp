using KerniX.Models.Ventas.Panel;
using KerniX.Services.Ventas.Panel;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.Ventas.Devoluciones;

public sealed class IndexModel : KerniX.Pages.Ventas.VentasPanelPageModel
{
    public IndexModel(IVentasPanelService service, ILogger<IndexModel> logger) : base(service, logger) { }

    public Task<IActionResult> OnGetAsync(string? buscar, string? estado, int pagina = 1,
        DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default)
        => CargarAsync(PanelTipo.Devoluciones, buscar, estado, pagina, desde, hasta, cancellationToken);

    public Task<IActionResult> OnGetExportarAsync(string? buscar, string? estado,
        DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default)
        => CsvAsync(PanelTipo.Devoluciones, buscar, estado, desde, hasta, cancellationToken);
}

