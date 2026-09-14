namespace KerniX.Models.Ventas.Cliente;

public sealed class VentaClienteBuscarResponse
{
    public int IdCliente { get; set; }

    public string NumeroDocumento { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public bool Estado { get; set; }
}