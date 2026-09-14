using KerniX.Api.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface IAlertaMantenimientoDbService
{
    Task<IReadOnlyList<AlertaMantenimientoResponse>> ListarAsync();

    Task<IReadOnlyList<AlertaMantenimientoResponse>> ListarPendientesAsync();

    Task<AlertaMantenimientoResponse?> ObtenerPorIdAsync(
        long idAlertaMantenimiento);

    Task<AlertaMantenimientoResponse> CrearAsync(
        AlertaMantenimientoCrearRequest request);

    Task<AlertaMantenimientoResponse> AtenderAsync(
        long idAlertaMantenimiento);

    Task<AlertaMantenimientoResponse> DescartarAsync(
        long idAlertaMantenimiento);

    Task<AlertaMantenimientoResponse> CrearDesdeErrorAsync(
        long idEjecucionMantenimiento);
}