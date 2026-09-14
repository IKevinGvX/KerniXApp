namespace KerniX.Api.Models.Configuracion.Mantenimiento;

public sealed class CopiaSeguridadResponse
{
    public long IdCopiaSeguridad { get; set; }
    public int IdEmpresa { get; set; }
    public int IdUsuario { get; set; }
    public string TipoCopia { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public long? TamanoBytes { get; set; }
    public string? HashArchivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}

public sealed class CopiaSeguridadIniciarRequest
{
    public int IdEmpresa { get; set; }
    public int IdUsuario { get; set; }
    public string TipoCopia { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}

public sealed class CopiaSeguridadCompletarRequest
{
    public long TamanoBytes { get; set; }
    public string? HashArchivo { get; set; }
    public string? Observacion { get; set; }
}