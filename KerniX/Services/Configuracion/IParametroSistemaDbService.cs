using KerniX.Api.Models.Configuracion.ParametroSistema;

namespace KerniX.Api.Services.Configuracion;

public interface IParametroSistemaDbService
{
    Task<IReadOnlyList<ParametroSistemaResponse>> ListarAsync(
        int idEmpresa);

    Task<ParametroSistemaResponse?> ObtenerPorIdAsync(
        int idParametro);

    Task<ParametroSistemaResponse?> ObtenerPorCodigoAsync(
        int idEmpresa,
        string codigo);

    Task<ParametroSistemaResponse> CrearAsync(
        ParametroSistemaCrearRequest request);

    Task<ParametroSistemaResponse> ActualizarAsync(
        int idParametro,
        ParametroSistemaActualizarRequest request);

    Task<ParametroSistemaResponse> ActualizarValorAsync(
        ParametroSistemaActualizarValorRequest request);

    Task<ParametroSistemaResponse> CambiarEstadoAsync(
        int idParametro,
        bool estado);
}