using KerniX.Models.Cotizacion;
using KerniX.Services.Cotizacion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Cotizacion;

public sealed class IndexModel
    : PageModel
{
    private readonly ICotizacionDbService
        _cotizacionDbService;

    public IndexModel(
        ICotizacionDbService cotizacionDbService)
    {
        _cotizacionDbService =
            cotizacionDbService;
    }


    public List<CotizacionListaItem>
        Cotizaciones
    { get; private set; }
        = new();


    public CotizacionResumen
        Resumen
    { get; private set; }
        = new();


    public List<CotizacionEstadoResumen>
        Estados
    { get; private set; }
        = new();


    public List<CotizacionActividadItem>
        ActividadReciente
    { get; private set; }
        = new();


    public List<CotizacionTopProducto>
        TopProductos
    { get; private set; }
        = new();


    public List<CotizacionTopCliente>
        TopClientes
    { get; private set; }
        = new();


    [BindProperty(SupportsGet = true)]
    public DateTime? FechaDesde { get; set; }


    [BindProperty(SupportsGet = true)]
    public DateTime? FechaHasta { get; set; }


    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }


    [BindProperty(SupportsGet = true)]
    public int? IdCliente { get; set; }


    public string? ErrorMensaje { get; private set; }


    public async Task OnGetAsync()
    {
        try
        {
            /*
             * Por ahora NULL permite probar
             * todas las empresas/sucursales.
             *
             * Después esto lo reemplazaremos
             * por los IDs guardados en sesión.
             */

            int? idEmpresa =
                null;

            int? idSucursal =
                null;


            /*
             * Si no vienen fechas, usamos
             * el mes actual.
             */

            FechaDesde ??=
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

            FechaHasta ??=
                DateTime.Today;


            Task<List<CotizacionListaItem>>
                tareaLista =
                    _cotizacionDbService
                        .ListarAsync(
                            idEmpresa,
                            idSucursal,
                            IdCliente,
                            Estado,
                            FechaDesde,
                            FechaHasta
                        );


            Task<CotizacionResumen>
                tareaResumen =
                    _cotizacionDbService
                        .ObtenerResumenAsync(
                            idEmpresa,
                            idSucursal,
                            FechaDesde,
                            FechaHasta
                        );


            Task<List<CotizacionEstadoResumen>>
                tareaEstados =
                    _cotizacionDbService
                        .ObtenerEstadosAsync(
                            idEmpresa,
                            idSucursal,
                            FechaDesde,
                            FechaHasta
                        );


            Task<List<CotizacionActividadItem>>
                tareaActividad =
                    _cotizacionDbService
                        .ObtenerActividadRecienteAsync(
                            idEmpresa,
                            idSucursal,
                            8
                        );


            Task<List<CotizacionTopProducto>>
                tareaProductos =
                    _cotizacionDbService
                        .ObtenerTopProductosAsync(
                            idEmpresa,
                            idSucursal,
                            FechaDesde,
                            FechaHasta,
                            5
                        );


            Task<List<CotizacionTopCliente>>
                tareaClientes =
                    _cotizacionDbService
                        .ObtenerTopClientesAsync(
                            idEmpresa,
                            idSucursal,
                            FechaDesde,
                            FechaHasta,
                            5
                        );


            await Task.WhenAll(
                tareaLista,
                tareaResumen,
                tareaEstados,
                tareaActividad,
                tareaProductos,
                tareaClientes
            );


            Cotizaciones =
                await tareaLista;

            Resumen =
                await tareaResumen;

            Estados =
                await tareaEstados;

            ActividadReciente =
                await tareaActividad;

            TopProductos =
                await tareaProductos;

            TopClientes =
                await tareaClientes;
        }
        catch (Exception ex)
        {
            ErrorMensaje =
                "No fue posible cargar las cotizaciones. "
                + ex.Message;

            Cotizaciones =
                new();

            Resumen =
                new();

            Estados =
                new();

            ActividadReciente =
                new();

            TopProductos =
                new();

            TopClientes =
                new();
        }
    }
}