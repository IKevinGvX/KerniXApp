using KerniX.Api.Models.Configuracion.Dashboard;

namespace KerniX.Api.Services.Configuracion;

public interface IConfiguracionDbService
{
    Task<ConfiguracionValidacionResponse> ValidarObligatoriosAsync(
        int idEmpresa,
        int idSucursal);

    Task<ConfiguracionDiagnosticoResponse> ObtenerDiagnosticoAsync(
        int idEmpresa,
        int idSucursal);
}