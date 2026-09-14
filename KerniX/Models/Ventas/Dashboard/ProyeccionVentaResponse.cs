namespace KerniX.Models.Ventas.Dashboard;

public sealed class ProyeccionVentaResponse
{
    public DateTime Fecha { get; set; }

    public string Etiqueta { get; set; }
        = string.Empty;

    public decimal VentaEstimada { get; set; }

    public decimal LimiteInferior { get; set; }

    public decimal LimiteSuperior { get; set; }

    public decimal Confianza { get; set; }
}