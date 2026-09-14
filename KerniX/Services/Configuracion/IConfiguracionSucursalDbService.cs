using KerniX.Api.Models.Configuracion.ConfiguracionSucursal;

namespace KerniX.Api.Services.Configuracion;

public interface IConfiguracionSucursalDbService
{
    Task<IReadOnlyList<ConfiguracionSucursalResponse>> ListarAsync(
        int idSucursal);

    Task<IReadOnlyList<ConfiguracionSucursalResponse>> ListarActivasAsync(
        int idSucursal);

    Task<ConfiguracionSucursalResponse?> ObtenerPorIdAsync(
        int idConfiguracionSucursal);

    Task<ConfiguracionSucursalResponse?> ObtenerPorCodigoAsync(
        int idSucursal,
        string codigo);

    Task<ConfiguracionSucursalResponse> CrearAsync(
        ConfiguracionSucursalCrearRequest request);

    Task<ConfiguracionSucursalResponse> ActualizarAsync(
        int idConfiguracionSucursal,
        ConfiguracionSucursalActualizarRequest request);

    Task<ConfiguracionSucursalResponse> ActualizarValorAsync(
        int idSucursal,
        string codigo,
        string? valor);

    Task<ConfiguracionSucursalResponse> CambiarEstadoAsync(
        int idConfiguracionSucursal,
        bool estado);

    Task<ConfiguracionSucursalResponse> GuardarAsync(
        ConfiguracionSucursalGuardarRequest request);
}