using KerniX.Models.Proveedores;

namespace KerniX.Services.Proveedores;

public interface IProveedorDbService
{
    Task<ProveedorListado> ListarAsync(int idEmpresa, string? buscar, bool? estado,
        int pagina, int tamanoPagina = 12, CancellationToken cancellationToken = default);

    Task<ProveedorItem?> ObtenerAsync(int idEmpresa, int idProveedor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProveedorEvento>> HistorialAsync(int idEmpresa, int idProveedor,
        CancellationToken cancellationToken = default);

    Task<int> CrearAsync(int idEmpresa, int idUsuario, ProveedorFormulario formulario,
        CancellationToken cancellationToken = default);

    Task<bool> ActualizarAsync(int idEmpresa, int idUsuario, int idProveedor, ProveedorFormulario formulario,
        CancellationToken cancellationToken = default);

    Task<bool> CambiarEstadoAsync(int idEmpresa, int idUsuario, int idProveedor, bool activo,
        CancellationToken cancellationToken = default);
}
