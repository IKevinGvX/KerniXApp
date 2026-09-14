namespace KerniX.DTOs.Empleados;

public sealed class EmpleadoDto
{
    public int IdEmpleado { get; set; }

    public string Nombre { get; set; }
        = string.Empty;

    public string ApellidoPaterno { get; set; }
        = string.Empty;

    public string? ApellidoMaterno { get; set; }

    public string? Cargo { get; set; }

    public string? Correo { get; set; }

    public bool Activo { get; set; }
}