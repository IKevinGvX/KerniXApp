namespace KerniX.Models.Cotizacion;

public sealed class ClienteCotizacionItem
{
    public int IdCliente { get; set; }

    public string TipoCliente { get; set; }
        = string.Empty;

    public string TipoDocumento { get; set; }
        = string.Empty;

    public string NumeroDocumento { get; set; }
        = string.Empty;

    public string Cliente { get; set; }
        = string.Empty;

    public string? RazonSocial { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }
}