namespace KerniX.Models.Cotizacion;

public sealed class CotizacionTopCliente
{
    public int IdCliente { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public int CantidadCotizaciones { get; set; }

    public decimal ImporteCotizado { get; set; }

    public DateTime UltimaCotizacion { get; set; }

    public int CotizacionesAprobadas { get; set; }

    public int CotizacionesConvertidas { get; set; }

    public decimal PorcentajeConversion
    {
        get
        {
            if (CantidadCotizaciones <= 0)
            {
                return 0;
            }

            return Math.Round(
                (
                    (decimal)CotizacionesConvertidas /
                    CantidadCotizaciones
                ) * 100,
                2
            );
        }
    }
}