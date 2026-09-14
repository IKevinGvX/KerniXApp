namespace KerniX.DTOs.Empleados;

public sealed class EmpleadoApiResponse
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public EmpleadoDto? Data { get; set; }
}