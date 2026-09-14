using System.Text.Json.Serialization;

namespace KerniX.Models.Usuarios;

public sealed class UsuarioPerfilResponse
{
    public int IdUsuario { get; set; }

    public int IdEmpleado { get; set; }

    public string NombreUsuario { get; set; }
        = string.Empty;

    public string Correo { get; set; }
        = string.Empty;

    public string Empleado { get; set; }
        = string.Empty;

    public string Empresa { get; set; }
        = string.Empty;

    public string Sucursal { get; set; }
        = string.Empty;

    public bool Estado { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public List<string> Roles { get; set; }
        = new();

    [JsonIgnore]
    public string RolesTexto =>
        Roles.Count == 0
            ? string.Empty
            : string.Join(
                ", ",
                Roles);
}