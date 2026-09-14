namespace KerniX.Models.Ventas.Cotizacion;

public sealed class CotizacionListadoResponse
{
    public long IdCotizacion { get; set; }

    public string NumeroCotizacion { get; set; }
        = string.Empty;

    public DateTime FechaCotizacion { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;
}