using KerniX.Models.Inventario.Enterprise;

namespace KerniX.Services.Inventario;

public interface IInventarioEnterpriseDbService
{
    Task<InventarioEnterpriseCatalogos> ObtenerCatalogosAsync(
        int idEmpresa,
        int? idSucursal = null,
        CancellationToken cancellationToken = default
    );

    Task<List<MovimientoReversibleItem>> ListarReversiblesAsync(
        int idEmpresa,
        int? idSucursal = null,
        int top = 100,
        CancellationToken cancellationToken = default
    );

    Task<ReversionMovimientoResultado> RevertirMovimientoAsync(
        long idMovimiento,
        int idEmpleado,
        string motivo,
        bool permitirDocumentoComercial = false,
        CancellationToken cancellationToken = default
    );

    Task<List<TransferenciaListaItem>> ListarTransferenciasAsync(
        int idEmpresa,
        string? estado = null,
        CancellationToken cancellationToken = default
    );

    Task<long> CrearTransferenciaAsync(
        int idEmpresa,
        int idAlmacenOrigen,
        int idAlmacenDestino,
        int idEmpleado,
        string? observacion = null,
        CancellationToken cancellationToken = default
    );

    Task AgregarDetalleTransferenciaAsync(
        long idTransferencia,
        int idProducto,
        decimal cantidad,
        CancellationToken cancellationToken = default
    );

    Task AprobarTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task DespacharTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task RecibirTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task CancelarTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        string motivo,
        CancellationToken cancellationToken = default
    );

    Task<TransferenciaDetalleResultado?> ObtenerTransferenciaAsync(
        long idTransferencia,
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task<List<InventarioFisicoListaItem>> ListarInventariosFisicosAsync(
        int idEmpresa,
        string? estado = null,
        CancellationToken cancellationToken = default
    );

    Task<long> IniciarInventarioFisicoAsync(
        int idEmpresa,
        int idAlmacen,
        int idEmpleado,
        string? observacion = null,
        CancellationToken cancellationToken = default
    );

    Task<InventarioFisicoDetalleResultado?> ObtenerInventarioFisicoAsync(
        long idInventarioFisico,
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task GuardarConteoFisicoAsync(
        long idInventarioFisico,
        int idProducto,
        decimal cantidadContada,
        CancellationToken cancellationToken = default
    );

    Task CerrarInventarioFisicoAsync(
        long idInventarioFisico,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task AplicarInventarioFisicoAsync(
        long idInventarioFisico,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task RegistrarAjusteAsync(
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal diferencia,
        int idEmpleado,
        string motivo,
        CancellationToken cancellationToken = default
    );

    Task<List<AjusteInventarioItem>> ListarAjustesAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task RegistrarMermaAsync(
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal cantidad,
        string causa,
        string motivo,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task<List<MermaItem>> ListarMermasAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task RegistrarDevolucionAsync(
        string tipo,
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal cantidad,
        string? origenTipo,
        long? origenId,
        string? documentoOrigen,
        string? terceroDocumento,
        string? terceroNombre,
        string motivo,
        int idEmpleado,
        CancellationToken cancellationToken = default
    );

    Task<List<DevolucionInventarioItem>> ListarDevolucionesAsync(
        int idEmpresa,
        string? tipo = null,
        CancellationToken cancellationToken = default
    );

    Task<KardexResultado> ListarKardexAsync(
        KardexFiltro filtro,
        CancellationToken cancellationToken = default
    );
}
