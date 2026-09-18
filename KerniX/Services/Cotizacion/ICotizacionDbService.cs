using KerniX.Models.Cotizacion;

namespace KerniX.Services.Cotizacion;

public interface ICotizacionDbService
{
    Task<List<CotizacionListaItem>>
        ListarAsync(
            int? idEmpresa,
            int? idSucursal,
            int? idCliente,
            string? estado,
            DateTime? fechaDesde,
            DateTime? fechaHasta);
    Task<List<AlmacenCotizacionItem>> ListarAlmacenesAsync(
    int idEmpresa,
    int idSucursal);
    Task<VentaVinculadaCotizacion?> ObtenerVentaVinculadaAsync(
    long idCotizacion,
    int idEmpresa,
    int idSucursal);
    Task<long> ConvertirVentaAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal,
        int idAlmacen);
    Task<CotizacionResumen>
        ObtenerResumenAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta);

    Task<List<CotizacionEstadoResumen>>
        ObtenerEstadosAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta);

    Task<List<CotizacionActividadItem>>
        ObtenerActividadRecienteAsync(
            int? idEmpresa,
            int? idSucursal,
            int top = 8);

    Task<List<CotizacionTopProducto>>
        ObtenerTopProductosAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int top = 5);

    Task<List<CotizacionTopCliente>>
        ObtenerTopClientesAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int top = 5);

    /*
 * =========================================================
 * DETALLE DE COTIZACIÓN
 * =========================================================
 */

    Task<CotizacionDetalleResponse?>
        ObtenerPorIdAsync(
            long idCotizacion,
            int idEmpresa,
            int? idSucursal = null);
    /*
     * =========================================================
     * NUEVA COTIZACIÓN
     * =========================================================
     */

    Task<List<ClienteCotizacionItem>>
        ListarClientesAsync(
            int idEmpresa);


    Task<List<ProductoCotizacionItem>>
        ListarProductosAsync(
            int idEmpresa,
            string? buscar = null);


    Task<CotizacionRegistroResultado>
        RegistrarAsync(
            NuevaCotizacionRequest request);
    // =========================================================
    // ESTADOS DE COTIZACIÓN
    // =========================================================

    Task AprobarAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal);

    Task RechazarAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal);
}