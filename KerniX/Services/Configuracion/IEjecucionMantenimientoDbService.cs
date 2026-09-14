using KerniX.Api.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface IEjecucionMantenimientoDbService
{
    Task<IReadOnlyList<EjecucionMantenimientoResponse>> ListarAsync();

    Task<EjecucionMantenimientoResponse?> ObtenerPorIdAsync(
        long idEjecucionMantenimiento);

    Task<IReadOnlyList<EjecucionMantenimientoResponse>> ListarPorPlanAsync(
        int idPlanMantenimiento);

    Task<EjecucionMantenimientoResponse> IniciarAsync(
        EjecucionMantenimientoIniciarRequest request);

    Task<EjecucionMantenimientoResponse> CompletarAsync(
        long idEjecucionMantenimiento,
        string? resultado);

    Task<EjecucionMantenimientoResponse> MarcarErrorAsync(
        long idEjecucionMantenimiento,
        string detalleError,
        string? resultado);

    Task<EjecucionMantenimientoResponse> CancelarAsync(
        long idEjecucionMantenimiento,
        string? resultado);
}