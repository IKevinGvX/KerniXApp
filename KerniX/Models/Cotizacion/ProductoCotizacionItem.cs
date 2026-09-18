namespace KerniX.Models.Cotizacion;

public sealed class ProductoCotizacionItem
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; }
        = string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioVenta { get; set; }

    public bool ControlaStock { get; set; }

    public bool Estado { get; set; }
}