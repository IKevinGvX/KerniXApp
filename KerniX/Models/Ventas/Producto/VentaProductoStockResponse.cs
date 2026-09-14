namespace KerniX.Models.Ventas.Producto;

public sealed class VentaProductoStockResponse
{
    public long IdStock { get; set; }

    public int IdAlmacen { get; set; }

    public int IdProducto { get; set; }

    public string Almacen { get; set; }
        = string.Empty;

    public string CodigoProducto { get; set; }
        = string.Empty;

    public string Producto { get; set; }
        = string.Empty;

    public decimal CantidadActual { get; set; }

    public decimal CantidadReservada { get; set; }

    public decimal CantidadDisponible
    {
        get
        {
            return
                CantidadActual -
                CantidadReservada;
        }
    }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public DateTime FechaActualizacion { get; set; }
}