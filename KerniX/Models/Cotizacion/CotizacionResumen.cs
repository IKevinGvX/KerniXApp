namespace KerniX.Models.Cotizacion;

public sealed class CotizacionResumen
{
    public int TotalCotizaciones { get; set; }

    public int Pendientes { get; set; }

    public int Aprobadas { get; set; }

    public int Convertidas { get; set; }

    public int Rechazadas { get; set; }

    public decimal ImporteTotal { get; set; }
}