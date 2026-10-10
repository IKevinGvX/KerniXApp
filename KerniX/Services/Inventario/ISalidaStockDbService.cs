using KerniX.Models.Inventario.SalidaStock;

namespace KerniX.Services.Inventario;

public interface ISalidaStockDbService
{
    Task<SalidaStockCatalogos> ObtenerCatalogosAsync(
        int idEmpresa,
        int? idSucursal = null,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default
    );

    Task<SalidaStockResultado> RegistrarAsync(
        SalidaStockGuardarRequest request,
        CancellationToken cancellationToken = default
    );

    Task<SalidaStockListaResultado> ListarAsync(
        SalidaStockFiltro filtro,
        CancellationToken cancellationToken = default
    );

    Task<SalidaStockResultado?> ObtenerAsync(
        long idMovimientoInventario,
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task<SalidaStockResumen> ObtenerResumenAsync(
        int idEmpresa,
        int? idSucursal = null,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default
    );

    Task<SalidaStockDisponible?> ObtenerStockDisponibleAsync(
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        CancellationToken cancellationToken = default
    );
}
