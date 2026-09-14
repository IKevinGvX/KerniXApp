namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaTopProductoResponse
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Producto { get; set; }
        = string.Empty;

    public decimal CantidadVendida { get; set; }

    public decimal TotalVenta { get; set; }

    public int Posicion { get; set; }
}