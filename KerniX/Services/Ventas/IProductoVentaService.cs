using KerniX.Models.Ventas.Venta;

namespace KerniX.Services.Ventas;

public interface IProductoVentaService
{
    Task<IReadOnlyList<ProductoVentaResponse>>
        BuscarAsync(
            string? termino,
            CancellationToken cancellationToken = default
        );

    Task<ProductoVentaResponse?>
        ObtenerPorIdAsync(
            int idProducto,
            CancellationToken cancellationToken = default
        );
}