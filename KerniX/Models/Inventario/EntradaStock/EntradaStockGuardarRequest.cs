namespace KerniX.Models.Inventario.EntradaStock;

public sealed class EntradaStockGuardarRequest
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdAlmacen { get; set; }

    public int IdProducto { get; set; }

    public int IdEmpleado { get; set; }

    public decimal Cantidad { get; set; }

    public string? Referencia { get; set; }

    public string? Motivo { get; set; }

    public string CodigoMovimiento { get; set; } =
        "entrada";
}
