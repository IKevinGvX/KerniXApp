namespace KerniX.Models.Ventas.Venta;

public sealed class VentaListadoResponse
{
    public long IdVenta { get; set; }

    public string NumeroVenta { get; set; }
        = string.Empty;

    public DateTime FechaVenta { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public string Vendedor { get; set; }
        = string.Empty;

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;
}