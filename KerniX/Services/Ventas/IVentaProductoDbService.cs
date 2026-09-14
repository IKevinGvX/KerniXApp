using KerniX.Models.Ventas.Producto;

namespace KerniX.Services.Ventas;

public interface IVentaProductoDbService
{
    Task<IReadOnlyList<VentaProductoBuscarResponse>> BuscarAsync(
        int idEmpresa,
        int idAlmacen,
        string? termino,
        int limite = 20);

    Task<VentaProductoResponse?> ObtenerPorIdAsync(
        int idEmpresa,
        int idProducto);

    Task<VentaProductoStockResponse?> ObtenerStockAsync(
        int idAlmacen,
        int idProducto);
}