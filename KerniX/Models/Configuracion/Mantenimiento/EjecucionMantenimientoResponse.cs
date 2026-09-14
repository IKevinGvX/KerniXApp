namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class EjecucionMantenimientoResponse
{
    public long IdEjecucionMantenimiento { get; set; }

    public int IdPlanMantenimiento { get; set; }

    public string? CodigoPlan { get; set; }

    public string? NombrePlan { get; set; }

    public string? TipoTarea { get; set; }

    public string OrigenEjecucion { get; set; } = string.Empty;

    public string? IdEjecucionExterna { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Resultado { get; set; }

    public string? DetalleError { get; set; }

    public int? DuracionSegundos { get; set; }
}