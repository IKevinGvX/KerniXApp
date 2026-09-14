namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class MantenimientoResultadoResponse
{
    public long IdEjecucionMantenimiento { get; set; }

    public int IdPlanMantenimiento { get; set; }

    public string CodigoPlan { get; set; } = string.Empty;

    public string NombrePlan { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Resultado { get; set; }
}