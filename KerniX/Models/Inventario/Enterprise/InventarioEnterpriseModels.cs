namespace KerniX.Models.Inventario.Enterprise;

public sealed class InventarioEnterpriseCatalogos
{
    public List<InventarioAlmacenItem> Almacenes { get; set; } = new();

    public List<InventarioProductoItem> Productos { get; set; } = new();
}


public sealed class InventarioAlmacenItem
{
    public int IdAlmacen { get; set; }

    public int IdSucursal { get; set; }

    public string Sucursal { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Ubicacion { get; set; }
}


public sealed class InventarioProductoItem
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }
}


public sealed class MovimientoReversibleItem
{
    public long IdMovimientoInventario { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public string CodigoMovimiento { get; set; } = string.Empty;

    public string TipoMovimiento { get; set; } = string.Empty;

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public string Almacen { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string? Referencia { get; set; }

    public bool PermiteReversionGenerica { get; set; }
}


public sealed class ReversionMovimientoResultado
{
    public long IdMovimientoOrigen { get; set; }

    public long IdMovimientoReversa { get; set; }

    public string CodigoOrigen { get; set; } = string.Empty;

    public string CodigoReversa { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal StockOrigenAnterior { get; set; }

    public decimal StockOrigenNuevo { get; set; }

    public decimal StockReversaAnterior { get; set; }

    public decimal StockReversaNuevo { get; set; }

    public DateTime FechaReversa { get; set; }

    public string Motivo { get; set; } = string.Empty;
}


public sealed class TransferenciaListaItem
{
    public long IdTransferencia { get; set; }

    public string NumeroTransferencia { get; set; } = string.Empty;

    public string AlmacenOrigen { get; set; } = string.Empty;

    public string AlmacenDestino { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaDespacho { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public int Lineas { get; set; }

    public decimal CantidadTotal { get; set; }
}


public sealed class TransferenciaDetalleResultado
{
    public long IdTransferencia { get; set; }

    public string NumeroTransferencia { get; set; } = string.Empty;

    public int IdEmpresa { get; set; }

    public int IdAlmacenOrigen { get; set; }

    public string AlmacenOrigen { get; set; } = string.Empty;

    public int IdAlmacenDestino { get; set; }

    public string AlmacenDestino { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaDespacho { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public List<TransferenciaDetalleItem> Detalles { get; set; } = new();
}


public sealed class TransferenciaDetalleItem
{
    public long IdTransferenciaDetalle { get; set; }

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public string Estado { get; set; } = string.Empty;

    public long? IdMovimientoSalida { get; set; }

    public long? IdMovimientoEntrada { get; set; }
}


public sealed class InventarioFisicoListaItem
{
    public long IdInventarioFisico { get; set; }

    public string NumeroInventario { get; set; } = string.Empty;

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public DateTime? FechaAplicacion { get; set; }

    public int TotalProductos { get; set; }

    public int PendientesConteo { get; set; }

    public decimal DiferenciaAbsoluta { get; set; }
}


public sealed class InventarioFisicoDetalleResultado
{
    public long IdInventarioFisico { get; set; }

    public string NumeroInventario { get; set; } = string.Empty;

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public DateTime? FechaAplicacion { get; set; }

    public List<InventarioFisicoDetalleItem> Detalles { get; set; } = new();
}


public sealed class InventarioFisicoDetalleItem
{
    public long IdInventarioFisicoDetalle { get; set; }

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public decimal StockSistema { get; set; }

    public decimal? CantidadContada { get; set; }

    public decimal? Diferencia { get; set; }

    public long? IdMovimientoAjuste { get; set; }
}


public sealed class AjusteInventarioItem
{
    public long IdAjusteInventario { get; set; }

    public string NumeroAjuste { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public decimal Diferencia { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public long? IdMovimientoInventario { get; set; }
}


public sealed class MermaItem
{
    public long IdMerma { get; set; }

    public string NumeroMerma { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public string Causa { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public long? IdMovimientoInventario { get; set; }
}


public sealed class DevolucionInventarioItem
{
    public long IdDevolucion { get; set; }

    public string NumeroDevolucion { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public string? OrigenTipo { get; set; }

    public long? OrigenId { get; set; }

    public string? DocumentoOrigen { get; set; }

    public string? TerceroDocumento { get; set; }

    public string? TerceroNombre { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public long? IdMovimientoInventario { get; set; }
}


public sealed class KardexFiltro
{
    public int IdEmpresa { get; set; }

    public int? IdSucursal { get; set; }

    public int? IdAlmacen { get; set; }

    public int? IdProducto { get; set; }

    public string? CodigoMovimiento { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Buscar { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanoPagina { get; set; } = 50;
}


public sealed class KardexResultado
{
    public List<KardexItem> Items { get; set; } = new();

    public long TotalRegistros { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanoPagina { get; set; } = 50;

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalRegistros /
                (double)TamanoPagina
            );
}


public sealed class KardexItem
{
    public long IdMovimientoInventario { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public int IdSucursal { get; set; }

    public string Sucursal { get; set; } = string.Empty;

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = string.Empty;

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } = string.Empty;

    public string Producto { get; set; } = string.Empty;

    public string CodigoMovimiento { get; set; } = string.Empty;

    public string TipoMovimiento { get; set; } = string.Empty;

    public string Naturaleza { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string? Referencia { get; set; }

    public string? Motivo { get; set; }

    public int IdEmpleado { get; set; }

    public string? Modulo { get; set; }

    public string? OrigenTipo { get; set; }

    public long? OrigenId { get; set; }

    public string? DocumentoOrigen { get; set; }

    public bool EstaRevertido { get; set; }

    public long? IdMovimientoReversa { get; set; }
}
