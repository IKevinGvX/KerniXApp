namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaActividadResponse
{
    public long? IdVenta { get; set; }

    public string Tipo { get; set; }
        = string.Empty;

    public string Titulo { get; set; }
        = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime Fecha { get; set; }

    public decimal? Monto { get; set; }
}