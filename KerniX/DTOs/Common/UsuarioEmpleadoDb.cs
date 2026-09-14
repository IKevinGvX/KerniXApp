namespace KerniX.Models.Auth;

public sealed class UsuarioEmpleadoDb
{
    public int IdUsuario { get; set; }

    public int? IdEmpleado { get; set; }

    public string Usuario { get; set; }
        = string.Empty;

    public string? Nombre { get; set; }

    public string? ApellidoPaterno { get; set; }

    public string? ApellidoMaterno { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Cargo { get; set; }

    public string? TipoDocumento { get; set; }

    public string? NumeroDocumento { get; set; }

    public int? IdEmpresa { get; set; }

    public int? IdSucursal { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public string NombreCompleto
    {
        get
        {
            return string.Join(
                " ",
                new[]
                {
                    Nombre,
                    ApellidoPaterno,
                    ApellidoMaterno
                }
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)));
        }
    }
}