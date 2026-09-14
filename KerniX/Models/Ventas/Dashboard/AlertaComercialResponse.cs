namespace KerniX.Models.Ventas.Dashboard;

public sealed class AlertaComercialResponse
{
    public int Id { get; set; }

    public string Tipo { get; set; }
        = string.Empty;

    public string Nivel { get; set; }
        = "Baja";

    public string Titulo { get; set; }
        = string.Empty;

    public string Descripcion { get; set; }
        = string.Empty;

    public string Entidad { get; set; }
        = string.Empty;

    public string Accion { get; set; }
        = string.Empty;

    public decimal? Valor { get; set; }

    public DateTime Fecha { get; set; }
}