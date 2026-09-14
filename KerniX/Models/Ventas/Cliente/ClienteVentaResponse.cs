namespace KerniX.Models.Ventas.Venta;

public sealed class ClienteVentaResponse
{
    public int IdCliente { get; set; }

    public int IdEmpresa { get; set; }

    public string Empresa { get; set; } = string.Empty;

    public string TipoCliente { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public string NumeroDocumento { get; set; } = string.Empty;

    public string? RazonSocial { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int TotalContactos { get; set; }

    public int TotalDirecciones { get; set; }

    public int TotalVentas { get; set; }


    public string NombreMostrar
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RazonSocial))
            {
                return RazonSocial;
            }

            if (!string.IsNullOrWhiteSpace(NombreCompleto))
            {
                return NombreCompleto;
            }

            return $"{Nombres} {Apellidos}".Trim();
        }
    }
}