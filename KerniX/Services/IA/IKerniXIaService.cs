using KerniX.Models.IA;

namespace KerniX.Services.IA;

public interface IKerniXIaService
{
    Task<IaContexto?> ObtenerContextoAsync(int idUsuario, CancellationToken ct = default);
    Task<IaAnalisisVentas> AnalizarVentasAsync(int idEmpresa, CancellationToken ct = default);
}
