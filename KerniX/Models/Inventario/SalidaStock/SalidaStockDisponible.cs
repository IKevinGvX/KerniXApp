namespace KerniX.Models.Inventario.SalidaStock;

public sealed class SalidaStockDisponible
{
    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } =
        string.Empty;

    public string Producto { get; set; } =
        string.Empty;

    public int IdAlmacen { get; set; }

    public string CodigoAlmacen { get; set; } =
        string.Empty;

    public string Almacen { get; set; } =
        string.Empty;

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public decimal StockDisponible { get; set; }
}
