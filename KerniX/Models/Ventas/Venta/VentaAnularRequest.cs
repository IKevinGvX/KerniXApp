namespace KerniX.Models.Ventas.Venta;

public sealed class VentaAnularRequest
{
    public long IdVenta { get; set; }

    public int IdEmpleado { get; set; }

    public int IdAlmacen { get; set; }

    public string Motivo { get; set; }
        = string.Empty;
}