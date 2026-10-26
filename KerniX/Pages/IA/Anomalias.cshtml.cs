using KerniX.Models.IA;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;

namespace KerniX.Pages.IA;

public sealed class AnomaliasModel : IaPageModelBase
{
    public AnomaliasModel(
        IKerniXIaService ia,
        ILogger<AnomaliasModel> logger)
        : base(ia, logger)
    {
    }


    public IaAnalisisVentas Analisis { get; private set; }
        = new();


    [BindProperty(SupportsGet = true)]
    public DateTime? FechaDesde { get; set; }


    [BindProperty(SupportsGet = true)]
    public DateTime? FechaHasta { get; set; }


    [BindProperty(SupportsGet = true)]
    public string Comparacion { get; set; }
        = "anterior";


    // =========================================================
    // CANTIDAD DE DÍAS DEL PERÍODO
    // =========================================================

    public int DiasPeriodo
    {
        get
        {
            if (!FechaDesde.HasValue ||
                !FechaHasta.HasValue)
            {
                return 0;
            }

            return
                (FechaHasta.Value.Date -
                 FechaDesde.Value.Date).Days + 1;
        }
    }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        // -----------------------------------------------------
        // VALIDAR USUARIO / EMPRESA
        // -----------------------------------------------------

        var resultado =
            await ValidarContextoAsync(
                cancellationToken);

        if (resultado != null)
        {
            return resultado;
        }


        if (Contexto is null)
        {
            return Page();
        }


        // =====================================================
        // FECHAS PREDETERMINADAS
        // =====================================================
        //
        // Si el usuario entra por primera vez:
        //
        // Hasta = hoy
        // Desde = hace 29 días
        //
        // Total = 30 días
        // =====================================================

        DateTime hoy =
            DateTime.Today;


        FechaHasta ??=
            hoy;


        FechaDesde ??=
            hoy.AddDays(-29);


        FechaDesde =
            FechaDesde.Value.Date;


        FechaHasta =
            FechaHasta.Value.Date;


        // =====================================================
        // VALIDACIONES
        // =====================================================

        if (FechaDesde.Value >
            FechaHasta.Value)
        {
            MensajeError =
                "La fecha inicial no puede ser mayor que la fecha final.";

            return Page();
        }


        if (FechaHasta.Value >
            hoy)
        {
            MensajeError =
                "La fecha final no puede ser posterior a la fecha actual.";

            return Page();
        }


        if (DiasPeriodo > 366)
        {
            MensajeError =
                "El período máximo permitido es de 366 días.";

            return Page();
        }


        // =====================================================
        // ANÁLISIS REAL
        // =====================================================

        try
        {
            Analisis =
                await Ia.AnalizarVentasAsync(
                    Contexto.IdEmpresa,
                    FechaDesde.Value,
                    FechaHasta.Value,
                    cancellationToken);
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            Log.LogError(
                ex,
                "[KERNIX IA] Error analizando ventas. Empresa={Empresa} Desde={Desde} Hasta={Hasta}",
                Contexto.IdEmpresa,
                FechaDesde,
                FechaHasta);


            MensajeError =
                "No fue posible analizar las ventas del período seleccionado.";
        }


        return Page();
    }
}