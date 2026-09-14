namespace KerniX.Api.Models.Configuracion.Impuesto;

public sealed class ImpuestoCrearRequest
{
    public int IdEmpresa { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Estado { get; set; } = true;
}