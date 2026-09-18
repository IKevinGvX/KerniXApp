namespace KerniX.Models.Cotizacion;

public sealed class NuevaCotizacionDetalleRequest
{
    public int IdProducto { get; set; }

    public string Producto { get; set; }
        = string.Empty;

    public string Codigo { get; set; }
        = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total
    {
        get
        {
            return
                (
                    Subtotal -
                    Descuento
                )
                +
                Impuesto;
        }
    }
}