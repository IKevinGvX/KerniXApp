using DocumentFormat.OpenXml.InkML;
using KerniX.Models.IA;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.IA;

public sealed class AnomaliasModel : IaPageModelBase
{
    public AnomaliasModel(IKerniXIaService ia, ILogger<AnomaliasModel> logger) : base(ia, logger) { }
    public IaAnalisisVentas Analisis { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await ValidarContextoAsync(cancellationToken);
        if (result != null) return result;
        if (Contexto is null) return Page();
        try { Analisis = await Ia.AnalizarVentasAsync(Contexto.IdEmpresa, cancellationToken); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.LogError(ex, "[KERNIX IA] Error al analizar ventas. Empresa={Empresa}", Contexto.IdEmpresa);
            MensajeError = "No fue posible analizar las ventas. Revisa la estructura SQL y los registros del servidor.";
        }
        return Page();
    }
}
