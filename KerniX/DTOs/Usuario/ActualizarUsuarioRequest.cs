namespace KerniX.Models.Usuarios;

public sealed class ActualizarUsuarioRequest
{
    public int IdEmpleado { get; set; }

    public string NombreUsuario { get; set; }
        = string.Empty;

    public string Correo { get; set; }
        = string.Empty;

    public bool Estado { get; set; }

    public bool RequiereCambioClave { get; set; }
}