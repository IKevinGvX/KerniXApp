namespace KerniX.Models.Cotizacion;

public sealed class CotizacionActividadItem
{
    public long IdCotizacion { get; set; }

    public string NumeroCotizacion { get; set; }
        = string.Empty;

    public string Cliente { get; set; }
        = string.Empty;

    public string Estado { get; set; }
        = string.Empty;

    public decimal Total { get; set; }

    public DateTime FechaEmision { get; set; }
}