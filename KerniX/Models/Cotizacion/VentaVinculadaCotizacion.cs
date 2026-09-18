namespace KerniX.Models.Cotizacion;

public sealed class VentaVinculadaCotizacion
{
    public long IdVenta { get; set; }

    public string NumeroVenta { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal Total { get; set; }
}