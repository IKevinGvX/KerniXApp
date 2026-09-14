using KerniX.Models.Ventas.Dashboard;

namespace KerniX.Services.Ventas;

public interface IVentaDashboardDbService
{
    Task<VentaDashboardResponse> ObtenerAsync(
        int idEmpresa,
        int? idSucursal = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null);
}