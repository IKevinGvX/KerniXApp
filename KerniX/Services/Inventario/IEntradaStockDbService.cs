using KerniX.Models.Inventario.EntradaStock;

namespace KerniX.Services.Inventario;

public interface IEntradaStockDbService
{
    Task<EntradaStockCatalogos> ObtenerCatalogosAsync(
        int idEmpresa,
        int? idSucursal = null,
        CancellationToken cancellationToken = default
    );

    Task<EntradaStockResultado> RegistrarAsync(
        EntradaStockGuardarRequest request,
        CancellationToken cancellationToken = default
    );

    Task<EntradaStockListaResultado> ListarAsync(
        EntradaStockFiltro filtro,
        CancellationToken cancellationToken = default
    );

    Task<EntradaStockResultado?> ObtenerAsync(
        long idMovimientoInventario,
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task<EntradaStockResumen> ObtenerResumenAsync(
        int idEmpresa,
        int? idSucursal = null,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default
    );
}
