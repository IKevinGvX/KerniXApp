
namespace KerniX.Models.Pedido;

// ============================================
// CABECERA Y DETALLE DEL PEDIDO
// ============================================

public sealed class PedidoDetalle
{
    public long IdPedido { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public long? IdCotizacion { get; set; }

    public string NumeroPedido { get; set; }
        = string.Empty;

    public DateTime FechaPedido { get; set; }

    public DateTime? FechaEntregaEstimada { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public string Vendedor { get; set; }
        = string.Empty;

    public string Sucursal { get; set; }
        = string.Empty;

    public string Estado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public List<PedidoDetalleProducto> Productos { get; set; }
        = new();

    // ========================================
    // INDICADORES
    // ========================================

    public decimal CantidadSolicitada =>
        Productos.Sum(x => x.CantidadSolicitada);

    public decimal CantidadAtendida =>
        Productos.Sum(x => x.CantidadAtendida);

    public decimal CantidadPendiente =>
        Productos.Sum(x =>
            Math.Max(
                0m,
                x.CantidadSolicitada - x.CantidadAtendida
            )
        );

    public decimal PorcentajeAtencion
    {
        get
        {
            if (CantidadSolicitada <= 0)
            {
                return 0m;
            }

            decimal porcentaje =
                CantidadAtendida
                / CantidadSolicitada
                * 100m;

            return Math.Clamp(
                porcentaje,
                0m,
                100m
            );
        }
    }
}


// ============================================
// PRODUCTOS DEL PEDIDO
// ============================================

public sealed class PedidoDetalleProducto
{
    public long IdDetallePedido { get; set; }

    public int IdProducto { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Producto { get; set; }
        = string.Empty;

    public decimal CantidadSolicitada { get; set; }

    public decimal CantidadAtendida { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public decimal CantidadPendiente =>
        Math.Max(
            0m,
            CantidadSolicitada - CantidadAtendida
        );
}