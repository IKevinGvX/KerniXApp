using KerniX.Models.Ventas.Venta;

namespace KerniX.Services.Ventas;

public interface IClienteVentaService
{
    Task<IReadOnlyList<ClienteVentaResponse>>
        BuscarAsync(
            string? termino,
            CancellationToken cancellationToken = default);


    Task<ClienteVentaResponse?>
        ObtenerPorIdAsync(
            int idCliente,
            CancellationToken cancellationToken = default);


    Task<ClienteVentaResponse?>
        CrearAsync(
            ClienteVentaCrearRequest request,
            CancellationToken cancellationToken = default);
}