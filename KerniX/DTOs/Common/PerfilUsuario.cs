public sealed class PerfilUsuario
{
    public int IdUsuario { get; set; }

    public string Usuario { get; set; }
        = string.Empty;

    public string NombreCompleto { get; set; }
        = string.Empty;

    public string Cargo { get; set; }
        = string.Empty;

    public string Correo { get; set; }
        = string.Empty;

    public string Estado { get; set; }
        = "Activo";

    public string Rol { get; set; }
        = string.Empty;

    public DateTime? UltimoAcceso { get; set; }
}