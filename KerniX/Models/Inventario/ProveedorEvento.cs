namespace KerniX.Models.Proveedores;

public sealed class ProveedorEvento
{
    public long IdEvento { get; set; }
    public int IdProveedor { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}
