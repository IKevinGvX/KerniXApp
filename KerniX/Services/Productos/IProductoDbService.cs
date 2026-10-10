using KerniX.Models.Inventario.Producto;

namespace KerniX.Services.Inventario;

public interface IProductoDbService
{
    Task<ProductoListaResultado> ListarAsync(
        ProductoFiltro filtro,
        CancellationToken cancellationToken = default
    );

    Task<ProductoDetalle?> ObtenerAsync(
        int idProducto,
        CancellationToken cancellationToken = default
    );

    Task<ProductoCatalogos> ObtenerCatalogosAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task<int> CrearAsync(
        ProductoGuardarRequest request,
        CancellationToken cancellationToken = default
    );

    Task ActualizarAsync(
        ProductoActualizarRequest request,
        CancellationToken cancellationToken = default
    );

    Task CambiarEstadoAsync(
        int idProducto,
        bool estado,
        int idUsuario,
        CancellationToken cancellationToken = default
    );

    Task<ProductoStockResultado> ObtenerStockAsync(
        int idProducto,
        int idEmpresa,
        CancellationToken cancellationToken = default
    );
}