namespace KerniX.Models.Ventas.Venta;

public sealed class VentaDetalleCrearRequest
{
    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }
}