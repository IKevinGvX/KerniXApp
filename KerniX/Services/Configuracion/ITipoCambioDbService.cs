using KerniX.Api.Models.Configuracion.TipoCambio;

namespace KerniX.Api.Services.Configuracion;

public interface ITipoCambioDbService
{
    Task<IReadOnlyList<TipoCambioResponse>> ListarAsync();

    Task<TipoCambioResponse?> ObtenerPorIdAsync(
        long idTipoCambio);

    Task<TipoCambioResponse?> ObtenerPorFechaAsync(
        int idMonedaOrigen,
        int idMonedaDestino,
        DateTime fecha);

    Task<TipoCambioResponse?> ObtenerUltimoAsync(
        int idMonedaOrigen,
        int idMonedaDestino);

    Task<IReadOnlyList<TipoCambioResponse>> ListarPorRangoAsync(
        int idMonedaOrigen,
        int idMonedaDestino,
        DateTime fechaDesde,
        DateTime fechaHasta);

    Task<TipoCambioResponse> CrearAsync(
        TipoCambioCrearRequest request);

    Task<TipoCambioResponse> ActualizarAsync(
        long idTipoCambio,
        TipoCambioActualizarRequest request);

    Task<TipoCambioResponse> GuardarAsync(
        TipoCambioCrearRequest request);
}