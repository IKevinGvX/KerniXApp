using KerniX.Models.Ventas.Panel;

namespace KerniX.Services.Ventas.Panel;

public interface IVentasPanelService
{
    Task<int?> ObtenerEmpresaDeUsuarioAsync(int idUsuario, CancellationToken ct = default);
    Task<VentasPanelVm> ObtenerAsync(PanelTipo tipo, int idEmpresa, string? buscar, string? estado,
        int pagina, DateTime? desde, DateTime? hasta, CancellationToken ct = default);
    Task<IReadOnlyList<PanelRegistro>> ExportarAsync(PanelTipo tipo, int idEmpresa,
        string? buscar, string? estado, DateTime? desde, DateTime? hasta, CancellationToken ct = default);
}

