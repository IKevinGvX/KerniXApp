namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class EjecucionMantenimientoIniciarRequest
{
    public int IdPlanMantenimiento { get; set; }

    public string OrigenEjecucion { get; set; } = "MANUAL";

    public string? IdEjecucionExterna { get; set; }
}

public sealed class EjecucionMantenimientoFinalizarRequest
{
    public string? Resultado { get; set; }

    public string? DetalleError { get; set; }
}