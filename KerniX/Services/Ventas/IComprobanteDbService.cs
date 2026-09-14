using KerniX.Models.Ventas.Comprobante;

namespace KerniX.Services.Ventas;

public interface IComprobanteDbService
{
    Task<ComprobanteResponse> EmitirAsync(
        ComprobanteCrearRequest request);

    Task<IReadOnlyList<ComprobanteResponse>> ListarPorVentaAsync(
        long idVenta);

    Task<ComprobanteResponse?> ObtenerPorIdAsync(
        long idComprobante);
}