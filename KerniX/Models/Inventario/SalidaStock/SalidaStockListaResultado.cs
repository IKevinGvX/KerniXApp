namespace KerniX.Models.Inventario.SalidaStock;

public sealed class SalidaStockListaResultado
{
    public List<SalidaStockListaItem> Items { get; set; } =
        new();

    public long TotalRegistros { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalRegistros /
                (double)TamanoPagina
            );
}


public sealed class SalidaStockListaItem
{
    public long IdMovimientoInventario { get; set; }

    public string NumeroSalida { get; set; } =
        string.Empty;

    public DateTime FechaMovimiento { get; set; }

    public int IdSucursal { get; set; }

    public string Sucursal { get; set; } =
        string.Empty;

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } =
        string.Empty;

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } =
        string.Empty;

    public string Producto { get; set; } =
        string.Empty;

    public string CodigoMovimiento { get; set; } =
        string.Empty;

    public string TipoMovimiento { get; set; } =
        string.Empty;

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string? Referencia { get; set; }

    public string? Motivo { get; set; }

    public int IdEmpleado { get; set; }
}
