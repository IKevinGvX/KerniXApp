using KerniX.Api.Models.Configuracion.Impuesto;

namespace KerniX.Api.Services.Configuracion;

public interface IImpuestoDbService
{
    Task<IReadOnlyList<ImpuestoResponse>> ListarAsync(
        int idEmpresa);

    Task<IReadOnlyList<ImpuestoResponse>> ListarActivosAsync(
        int idEmpresa);

    Task<IReadOnlyList<ImpuestoResponse>> ListarVigentesAsync(
        int idEmpresa,
        DateTime? fecha = null);

    Task<ImpuestoResponse?> ObtenerPorIdAsync(
        int idImpuesto);

    Task<ImpuestoResponse?> ObtenerPorCodigoAsync(
        int idEmpresa,
        string codigo);

    Task<ImpuestoResponse?> ObtenerVigenteAsync(
        int idEmpresa,
        string codigo);

    Task<ImpuestoResponse> CrearAsync(
        ImpuestoCrearRequest request);

    Task<ImpuestoResponse> ActualizarAsync(
        int idImpuesto,
        ImpuestoActualizarRequest request);

    Task<ImpuestoResponse> CambiarEstadoAsync(
        int idImpuesto,
        bool estado);

    Task<ImpuestoResponse> CerrarVigenciaAsync(
        int idImpuesto,
        DateTime fechaFin);
}