namespace KerniX.Models.Ventas.Cotizacion;

public sealed class CotizacionDetalleRequest
{
    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }
}