using KerniX.Models.Ventas.Pago;

namespace KerniX.Services.Ventas;

public interface IPagoDbService
{
    Task<PagoResponse> RegistrarAsync(
        PagoCrearRequest request);

    Task<IReadOnlyList<PagoResponse>> ListarPorVentaAsync(
        long idVenta);

    Task<PagoResumenResponse> ObtenerResumenAsync(
        long idVenta);
}