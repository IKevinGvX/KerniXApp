namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class PlanMantenimientoCrearRequest
{
    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public string? Descripcion { get; set; }

    public string TipoTarea { get; set; }
        = string.Empty;

    public string ProcedimientoEjecutar { get; set; }
        = string.Empty;

    public string? Parametros { get; set; }

    public string Frecuencia { get; set; }
        = string.Empty;

    public byte? DiaSemana { get; set; }

    public byte? DiaMes { get; set; }

    public TimeSpan? HoraEjecucion { get; set; }

    public bool PermiteEjecucionManual { get; set; }
        = true;

    public bool Estado { get; set; }
        = true;
}