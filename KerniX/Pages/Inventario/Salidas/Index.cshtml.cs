using KerniX.Models.Inventario.SalidaStock;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Salidas;

public sealed class IndexModel : PageModel
{
    private readonly ISalidaStockDbService _salidaStockService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISalidaStockDbService salidaStockService,
        ILogger<IndexModel> logger)
    {
        _salidaStockService = salidaStockService;
        _logger = logger;
    }


    [BindProperty(SupportsGet = true)]
    public SalidaStockFiltro Filtro { get; set; } = new();


    public SalidaStockListaResultado Resultado { get; private set; } =
        new();

    public SalidaStockCatalogos Catalogos { get; private set; } =
        new();

    public SalidaStockResumen Resumen { get; private set; } =
        new();


    public int IdUsuario { get; private set; }

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }


    public string? MensajeError { get; private set; }


    [TempData]
    public string? SalidaStockExito { get; set; }


    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        NormalizarFiltro();


        try
        {
            Catalogos =
                await _salidaStockService
                    .ObtenerCatalogosAsync(
                        IdEmpresa,
                        IdSucursal,
                        Filtro.IdAlmacen,
                        cancellationToken
                    );


            Resultado =
                await _salidaStockService
                    .ListarAsync(
                        Filtro,
                        cancellationToken
                    );


            Resumen =
                await _salidaStockService
                    .ObtenerResumenAsync(
                        IdEmpresa,
                        IdSucursal,
                        Filtro.IdAlmacen,
                        cancellationToken
                    );


            _logger.LogInformation(
                "[SALIDAS] Página cargada. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal} Registros={Registros} Cantidad={Cantidad}",
                IdUsuario,
                IdEmpresa,
                IdSucursal,
                Resultado.TotalRegistros,
                Resumen.CantidadRetirada
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[SALIDAS] ERROR SQL. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber
            );


            MensajeError =
                $"SQL {ex.Number} | {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SALIDAS] Error cargando listado."
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    private void NormalizarFiltro()
    {
        Filtro ??=
            new SalidaStockFiltro();


        // Contexto sensible siempre desde sesión.
        Filtro.IdEmpresa =
            IdEmpresa;

        Filtro.IdSucursal =
            IdSucursal;


        Filtro.Pagina =
            Filtro.Pagina <= 0
                ? 1
                : Filtro.Pagina;


        Filtro.TamanoPagina =
            Filtro.TamanoPagina <= 0
                ? 20
                : Math.Min(
                    Filtro.TamanoPagina,
                    100
                );


        Filtro.Buscar =
            string.IsNullOrWhiteSpace(
                Filtro.Buscar
            )
                ? null
                : Filtro.Buscar.Trim();


        if (
            Filtro.IdAlmacen.HasValue
            &&
            Filtro.IdAlmacen.Value <= 0
        )
        {
            Filtro.IdAlmacen =
                null;
        }


        if (
            Filtro.IdProducto.HasValue
            &&
            Filtro.IdProducto.Value <= 0
        )
        {
            Filtro.IdProducto =
                null;
        }


        if (
            Filtro.FechaDesde.HasValue
            &&
            Filtro.FechaHasta.HasValue
            &&
            Filtro.FechaDesde.Value.Date
            >
            Filtro.FechaHasta.Value.Date
        )
        {
            DateTime temporal =
                Filtro.FechaDesde.Value;


            Filtro.FechaDesde =
                Filtro.FechaHasta.Value;


            Filtro.FechaHasta =
                temporal;
        }
    }


    private bool CargarContextoSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );


        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );


        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );


        if (IdUsuario <= 0)
        {
            return false;
        }


        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }


        return true;
    }


    private int ObtenerEnteroSesion(
        string clave)
    {
        int? entero =
            HttpContext
                .Session
                .GetInt32(
                    clave
                );


        if (entero.HasValue)
        {
            return entero.Value;
        }


        string? texto =
            HttpContext
                .Session
                .GetString(
                    clave
                );


        return int.TryParse(
            texto,
            out int resultado
        )
            ? resultado
            : 0;
    }
}
