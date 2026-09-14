namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class AlertaMantenimientoResponse
{
    public long IdAlertaMantenimiento { get; set; }

    public long? IdEjecucionMantenimiento { get; set; }

    public string TipoAlerta { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }

    public DateTime? FechaAtencion { get; set; }
}

public sealed class AlertaMantenimientoCrearRequest
{
    public long? IdEjecucionMantenimiento { get; set; }

    public string TipoAlerta { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}