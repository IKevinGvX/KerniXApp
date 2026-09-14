namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaGraficoResponse
{
    public string Etiqueta { get; set; }
        = string.Empty;

    public DateTime? Fecha { get; set; }

    public decimal Total { get; set; }

    public int CantidadVentas { get; set; }
}