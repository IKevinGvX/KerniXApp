namespace KerniX.Models.Inventario.SalidaStock;

public sealed class SalidaStockResultado
{
    public long IdMovimientoInventario { get; set; }

    public string NumeroSalida { get; set; } =
        string.Empty;

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public string? Sucursal { get; set; }

    public int IdAlmacen { get; set; }

    public string CodigoAlmacen { get; set; } =
        string.Empty;

    public string Almacen { get; set; } =
        string.Empty;

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; } =
        string.Empty;

    public string Producto { get; set; } =
        string.Empty;

    public int? IdUnidadMedida { get; set; }

    public int IdTipoMovimiento { get; set; }

    public string CodigoMovimiento { get; set; } =
        string.Empty;

    public string TipoMovimiento { get; set; } =
        string.Empty;

    public string? Naturaleza { get; set; }

    public int IdEmpleado { get; set; }

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string? Referencia { get; set; }

    public string? Motivo { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public decimal CantidadActual { get; set; }

    public decimal CantidadReservada { get; set; }

    public decimal CantidadDisponible { get; set; }
}
