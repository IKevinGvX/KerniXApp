namespace KerniX.Models.Ventas.Pedido;

public sealed class PedidoDetalleRequest
{
    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }
}