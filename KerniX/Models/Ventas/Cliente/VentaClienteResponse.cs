namespace KerniX.Models.Ventas.Cliente;

public sealed class VentaClienteResponse
{
    public int IdCliente { get; set; }

    public int IdEmpresa { get; set; }

    public string TipoCliente { get; set; }
        = string.Empty;

    public string TipoDocumento { get; set; }
        = string.Empty;

    public string NumeroDocumento { get; set; }
        = string.Empty;

    public string? RazonSocial { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public bool Estado { get; set; }

    public string NombreCompleto
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                RazonSocial))
            {
                return RazonSocial;
            }

            return $"{Nombres} {Apellidos}"
                .Trim();
        }
    }
}