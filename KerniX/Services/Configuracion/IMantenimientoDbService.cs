using KerniX.Api.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface IMantenimientoDbService
{
    Task<MantenimientoResultadoResponse> EjecutarPlanAsync(
        int idPlanMantenimiento,
        MantenimientoEjecutarRequest request);

    Task<MantenimientoResultadoResponse> EjecutarPorCodigoAsync(
        string codigo,
        MantenimientoEjecutarRequest request);

    Task<EjecucionMantenimientoResponse?>
        ObtenerResultadoAsync(
            long idEjecucionMantenimiento);
}