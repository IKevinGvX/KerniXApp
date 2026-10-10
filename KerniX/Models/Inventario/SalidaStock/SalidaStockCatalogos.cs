namespace KerniX.Models.Inventario.SalidaStock;

public sealed class SalidaStockCatalogos
{
    public List<SalidaStockAlmacenItem> Almacenes { get; set; } =
        new();

    public List<SalidaStockProductoItem> Productos { get; set; } =
        new();

    public List<SalidaStockTipoMovimientoItem> TiposMovimiento { get; set; } =
        new();
}


public sealed class SalidaStockAlmacenItem
{
    public int IdAlmacen { get; set; }

    public int IdSucursal { get; set; }

    public int IdEmpresa { get; set; }

    public string Sucursal { get; set; } =
        string.Empty;

    public string Codigo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string? Ubicacion { get; set; }
}


public sealed class SalidaStockProductoItem
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public int IdCategoriaProducto { get; set; }

    public int? IdMarca { get; set; }

    public int IdUnidadMedida { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public bool ControlaStock { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public decimal StockDisponible { get; set; }
}


public sealed class SalidaStockTipoMovimientoItem
{
    public int IdTipoMovimiento { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string Naturaleza { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }
}
