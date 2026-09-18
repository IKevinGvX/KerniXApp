namespace KerniX.Models.Cotizacion;

public sealed class CotizacionEstadoResumen
{
    public string Estado { get; set; }
        = string.Empty;

    public int Cantidad { get; set; }

    public decimal TotalImporte { get; set; }
}