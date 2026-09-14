namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class MantenimientoEjecutarRequest
{
    public string OrigenEjecucion { get; set; } = "MANUAL";

    public string? IdEjecucionExterna { get; set; }

    public string? RutaBackup { get; set; }
}