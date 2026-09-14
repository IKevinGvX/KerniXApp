namespace KerniX.Models.Ventas.Producto;

public sealed class VentaProductoBuscarResponse
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; }
        = string.Empty;

    public decimal PrecioVenta { get; set; }

    public bool ControlaStock { get; set; }

    public decimal StockDisponible { get; set; }
}