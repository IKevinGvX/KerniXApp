namespace KerniX.Models.Cotizacion;

public sealed class CotizacionTopProducto
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public decimal TotalCotizado { get; set; }

    public int VecesCotizado { get; set; }

    public decimal ImporteCotizado { get; set; }
}