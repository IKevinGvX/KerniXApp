using KerniX.Api.Models.Configuracion.Mantenimiento;

namespace KerniX.Api.Services.Configuracion;

public interface ICopiaSeguridadDbService
{
    Task<IReadOnlyList<CopiaSeguridadResponse>> ListarAsync(
        int idEmpresa);

    Task<CopiaSeguridadResponse?> ObtenerPorIdAsync(
        long idCopiaSeguridad);

    Task<CopiaSeguridadResponse?> ObtenerUltimaAsync(
        int idEmpresa);

    Task<CopiaSeguridadResponse> IniciarAsync(
        CopiaSeguridadIniciarRequest request);

    Task<CopiaSeguridadResponse> CompletarAsync(
        long idCopiaSeguridad,
        CopiaSeguridadCompletarRequest request);

    Task<CopiaSeguridadResponse> MarcarErrorAsync(
        long idCopiaSeguridad,
        string observacion);

    Task<CopiaSeguridadResponse> CancelarAsync(
        long idCopiaSeguridad,
        string? observacion);
}