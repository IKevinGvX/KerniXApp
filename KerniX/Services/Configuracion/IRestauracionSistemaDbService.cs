using KerniX.Api.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface IRestauracionSistemaDbService
{
    Task<IReadOnlyList<RestauracionSistemaResponse>> ListarAsync();

    Task<RestauracionSistemaResponse?> ObtenerPorIdAsync(
        long idRestauracion);

    Task<RestauracionSistemaResponse> IniciarAsync(
        RestauracionIniciarRequest request);

    Task<RestauracionSistemaResponse> CompletarAsync(
        long idRestauracion,
        RestauracionFinalizarRequest request);

    Task<RestauracionSistemaResponse> MarcarErrorAsync(
        long idRestauracion,
        RestauracionFinalizarRequest request);

    Task<RestauracionSistemaResponse> CancelarAsync(
        long idRestauracion,
        string? observacion);
}