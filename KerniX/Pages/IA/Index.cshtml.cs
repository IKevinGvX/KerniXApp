using KerniX.Models.IA;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.IA;

public sealed class IndexModel : IaPageModelBase
{
    public IndexModel(
        IKerniXIaService ia,
        ILogger<IndexModel> logger)
        : base(ia, logger)
    {
    }

    public IaAnalisisVentas Analisis { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        var resultado =
            await ValidarContextoAsync(cancellationToken);

        if (resultado != null)
        {
            return resultado;
        }

        if (Contexto is null)
        {
            return Page();
        }

        try
        {
            Analisis =
                await Ia.AnalizarVentasAsync(
                    Contexto.IdEmpresa,
                    cancellationToken);
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            Log.LogError(
                ex,
                "[KERNIX IA] Error cargando panel de anomalías. Empresa={Empresa}",
                Contexto.IdEmpresa);

            MensajeError =
                "No fue posible cargar el análisis de ventas.";
        }

        return Page();
    }
}