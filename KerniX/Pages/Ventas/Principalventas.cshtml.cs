using KerniX.Models.Ventas.Dashboard;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Ventas;

public sealed class PrincipalVentasModel : PageModel
{
    private readonly IVentaDashboardDbService
        _dashboardService;

    private readonly ILogger<PrincipalVentasModel>
        _logger;

    public PrincipalVentasModel(
        IVentaDashboardDbService dashboardService,
        ILogger<PrincipalVentasModel> logger)
    {
        _dashboardService =
            dashboardService;

        _logger =
            logger;
    }

    public VentaDashboardResponse Dashboard { get; private set; }
        = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdSucursal { get; set; }

    public int IdEmpresa { get; private set; }

    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        IdEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? 1;

        if (!IdSucursal.HasValue ||
            IdSucursal.Value <= 0)
        {
            IdSucursal =
                HttpContext.Session.GetInt32(
                    "IdSucursal")
                ?? 1;
        }

        FechaHasta ??=
            DateTime.Today;

        FechaDesde ??=
            DateTime.Today.AddDays(-29);

        if (FechaDesde.Value.Date >
            FechaHasta.Value.Date)
        {
            MensajeError =
                "La fecha inicial no puede ser mayor que la fecha final.";

            FechaDesde =
                DateTime.Today.AddDays(-29);

            FechaHasta =
                DateTime.Today;
        }

        try
        {
            Dashboard =
                await _dashboardService.ObtenerAsync(
                    IdEmpresa,
                    IdSucursal,
                    FechaDesde,
                    FechaHasta);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando PrincipalVentas. " +
                "Empresa={IdEmpresa} | " +
                "Sucursal={IdSucursal}",
                IdEmpresa,
                IdSucursal);

            MensajeError =
                "No se pudo cargar la información de ventas.";

            Dashboard =
                new VentaDashboardResponse();

            return Page();
        }
    }
}