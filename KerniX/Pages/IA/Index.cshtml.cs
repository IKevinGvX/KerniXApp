using KerniX.Models.IA;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.IA;

public sealed class IndexModel : IaPageModelBase
{
    public IndexModel(IKerniXIaService ia, ILogger<IndexModel> logger) : base(ia, logger) { }
    public IaAnalisisVentas Analisis { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await ValidarContextoAsync(cancellationToken);
        if (result != null) return result;
        if (Contexto is null) return Page();
        try { Analisis = await Ia.AnalizarVentasAsync(Contexto.IdEmpresa, cancellationToken); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.LogError(ex, "[KERNIX IA] Error consultando ventas empresa {Empresa}", Contexto.IdEmpresa);
            MensajeError = "La analítica de ventas no pudo cargarse. Comprueba la tabla ven.venta y el log del servidor.";
        }
        return Page();
    }
}
