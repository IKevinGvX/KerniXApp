using KerniX.Api.Models.Configuracion.Moneda;

namespace KerniX.Api.Services.Configuracion;

public interface IMonedaDbService
{
    Task<IReadOnlyList<MonedaResponse>> ListarAsync();

    Task<IReadOnlyList<MonedaResponse>> ListarActivasAsync();

    Task<MonedaResponse?> ObtenerPorIdAsync(
        int idMoneda);

    Task<MonedaResponse?> ObtenerPorCodigoAsync(
        string codigo);

    Task<MonedaResponse> CrearAsync(
        MonedaCrearRequest request);

    Task<MonedaResponse> ActualizarAsync(
        int idMoneda,
        MonedaActualizarRequest request);

    Task<MonedaResponse> CambiarEstadoAsync(
        int idMoneda,
        bool estado);

    Task<bool> TieneDependenciasAsync(
        int idMoneda);
}