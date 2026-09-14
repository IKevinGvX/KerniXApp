namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class PlanMantenimientoResponse
{
    public int IdPlanMantenimiento { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string TipoTarea { get; set; } = string.Empty;

    public string ProcedimientoEjecutar { get; set; } = string.Empty;

    public string? Parametros { get; set; }

    public string Frecuencia { get; set; } = string.Empty;

    public byte? DiaSemana { get; set; }

    public byte? DiaMes { get; set; }

    public TimeSpan? HoraEjecucion { get; set; }

    public bool PermiteEjecucionManual { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }
}