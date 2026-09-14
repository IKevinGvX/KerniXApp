using KerniX.Api.Models.Configuracion.Mantenimiento;
using KerniX.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface IPlanMantenimientoDbService
{
    Task<IReadOnlyList<PlanMantenimientoResponse>> ListarAsync();

    Task<IReadOnlyList<PlanMantenimientoResponse>> ListarActivosAsync();

    Task<IReadOnlyList<PlanMantenimientoResponse>> ListarManualesAsync();

    Task<PlanMantenimientoResponse?> ObtenerPorIdAsync(
        int idPlanMantenimiento);

    Task<PlanMantenimientoResponse?> ObtenerPorCodigoAsync(
        string codigo);

    Task<PlanMantenimientoResponse> CrearAsync(
        PlanMantenimientoCrearRequest request);

    Task<PlanMantenimientoResponse> ActualizarAsync(
        int idPlanMantenimiento,
        PlanMantenimientoActualizarRequest request);

    Task<PlanMantenimientoResponse> CambiarEstadoAsync(
        int idPlanMantenimiento,
        bool estado);
}