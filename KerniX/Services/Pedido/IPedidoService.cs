
using KerniX.Models.Pedido;

namespace KerniX.Services.Pedido;

public interface IPedidoService
{
    // ========================================
    // LISTAR
    // ========================================

    Task<IReadOnlyList<PedidoFila>> ListarAsync(
        PedidoFiltro filtro,
        CancellationToken cancellationToken = default
    );

    // ========================================
    // REGISTRAR
    // ========================================

    Task<long> RegistrarAsync(
        PedidoRegistrarRequest request,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    // ========================================
    // BUSCADORES
    // ========================================

    Task<IReadOnlyList<PedidoClienteBusqueda>> BuscarClientesAsync(
        string buscar,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PedidoProductoBusqueda>> BuscarProductosAsync(
        string buscar,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PedidoCotizacionBusqueda>> BuscarCotizacionesAsync(
        int idCliente,
        CancellationToken cancellationToken = default
    );

    // ========================================
    // OBTENER DETALLE
    // ========================================

    Task<PedidoDetalle?> ObtenerAsync(
        long idPedido,
        int idEmpresa,
        int idSucursal,
        CancellationToken cancellationToken = default
    );

    // ========================================
    // APROBAR / CANCELAR
    // ========================================

    Task CambiarEstadoAsync(
        long idPedido,
        int idEmpresa,
        int idSucursal,
        string nuevoEstado,
        CancellationToken cancellationToken = default
    );
}