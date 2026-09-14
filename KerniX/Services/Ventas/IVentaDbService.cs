using KerniX.Models.Ventas.Venta;

namespace KerniX.Services.Ventas;

public interface IVentaDbService
{
    Task<VentaResponse> RegistrarAsync(
        VentaCrearRequest request);

    Task<VentaResponse?> ObtenerAsync(
        long idVenta);

    Task AnularAsync(
        VentaAnularRequest request);
}