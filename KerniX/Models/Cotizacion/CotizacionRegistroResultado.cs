namespace KerniX.Models.Cotizacion;

public sealed class CotizacionRegistroResultado
{
    public long IdCotizacion { get; set; }

    public string NumeroCotizacion { get; set; }
        = string.Empty;

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;
}