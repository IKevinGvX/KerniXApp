namespace KerniX.Api.Models.Configuracion.Moneda;

public sealed class MonedaCrearRequest
{
    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public string? Simbolo { get; set; }

    public int CantidadDecimales { get; set; } = 2;

    public bool Estado { get; set; } = true;
}