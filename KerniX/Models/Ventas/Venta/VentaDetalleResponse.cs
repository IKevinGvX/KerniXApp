namespace KerniX.Models.Ventas.Venta;

public sealed class VentaDetalleResponse
{
    public long IdDetalleVenta { get; set; }

    public long IdVenta { get; set; }

    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Producto { get; set; }
        = string.Empty;
}