using KerniX.Models.IA;

namespace KerniX.Services.IA;

public interface IKerniXIaService
{
    Task<IaContexto?> ObtenerContextoAsync(
        int idUsuario,
        CancellationToken ct = default);

    // Mantiene compatibilidad con IA/Index y otras páginas existentes.
    Task<IaAnalisisVentas> AnalizarVentasAsync(
        int idEmpresa,
        CancellationToken ct = default);

    // Análisis por período seleccionado.
    Task<IaAnalisisVentas> AnalizarVentasAsync(
        int idEmpresa,
        DateTime fechaDesde,
        DateTime fechaHasta,
        CancellationToken ct = default);
}