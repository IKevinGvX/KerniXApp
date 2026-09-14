namespace KerniX.Models.Ventas.Venta;

public sealed class ClienteVentaCrearRequest
{
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

    public List<ClienteContactoCrearRequest> Contactos
    {
        get;
        set;
    } = new();

    public List<ClienteDireccionCrearRequest> Direcciones
    {
        get;
        set;
    } = new();
}


public sealed class ClienteContactoCrearRequest
{
    public string? Nombres { get; set; }

    public string? Cargo { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public bool EsPrincipal { get; set; } = true;

    public bool Estado { get; set; } = true;
}


public sealed class ClienteDireccionCrearRequest
{
    public string? TipoDireccion { get; set; }

    public string? Direccion { get; set; }

    public string? Referencia { get; set; }

    public string? Distrito { get; set; }

    public string? Provincia { get; set; }

    public string? Departamento { get; set; }

    public string? Ubigeo { get; set; }

    public bool EsPrincipal { get; set; } = true;

    public bool Estado { get; set; } = true;
}