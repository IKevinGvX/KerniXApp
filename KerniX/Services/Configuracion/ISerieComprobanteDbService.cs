using KerniX.Api.Models.Configuracion.SerieComprobante;

namespace KerniX.Api.Services.Configuracion;

public interface ISerieComprobanteDbService
{
    Task<IReadOnlyList<SerieComprobanteResponse>> ListarAsync(
        int idEmpresa,
        int idSucursal);

    Task<IReadOnlyList<SerieComprobanteResponse>> ListarActivasAsync(
        int idEmpresa,
        int idSucursal);

    Task<SerieComprobanteResponse?> ObtenerPorIdAsync(
        int idSerieComprobante);

    Task<SerieComprobanteResponse?> ObtenerAsync(
        int idEmpresa,
        int idSucursal,
        string tipoComprobante,
        string serie);

    Task<SerieComprobanteResponse> CrearAsync(
        SerieComprobanteCrearRequest request);

    Task<SerieComprobanteResponse> ActualizarAsync(
        int idSerieComprobante,
        SerieComprobanteActualizarRequest request);

    Task<SerieComprobanteResponse> CambiarEstadoAsync(
        int idSerieComprobante,
        bool estado);

    Task<SerieComprobanteResponse> ActualizarCorrelativoAsync(
        int idSerieComprobante,
        long nuevoCorrelativo);

    Task<SerieComprobanteSiguienteResponse> ObtenerSiguienteAsync(
        int idEmpresa,
        int idSucursal,
        string tipoComprobante,
        string serie);

    Task<SerieComprobanteSiguienteResponse> GenerarNumeroAsync(
        int idEmpresa,
        int idSucursal,
        string tipoComprobante,
        string serie);
}