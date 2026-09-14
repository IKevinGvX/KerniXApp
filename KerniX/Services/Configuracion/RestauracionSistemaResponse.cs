namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class RestauracionSistemaResponse
{
    public long IdRestauracion { get; set; }
    public long IdCopiaSeguridad { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public string? Observacion { get; set; }
}

public sealed class RestauracionIniciarRequest
{
    public long IdCopiaSeguridad { get; set; }
    public int IdUsuario { get; set; }
    public string? Observacion { get; set; }
}

public sealed class RestauracionFinalizarRequest
{
    public string? Resultado { get; set; }
    public string? Observacion { get; set; }
}