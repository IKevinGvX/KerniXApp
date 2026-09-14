namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaIaInsightResponse
{
    public string Tipo { get; set; }
        = string.Empty;

    public string Titulo { get; set; }
        = string.Empty;

    public string Mensaje { get; set; }
        = string.Empty;

    public string Nivel { get; set; }
        = "INFO";

    public decimal? Valor { get; set; }

    public decimal? Variacion { get; set; }

    public string? Accion { get; set; }
}