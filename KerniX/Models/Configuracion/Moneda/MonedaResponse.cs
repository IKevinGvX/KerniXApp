namespace KerniX.Api.Models.Configuracion.Moneda;

public sealed class MonedaResponse
{
    public int IdMoneda { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public string? Simbolo { get; set; }

    public int CantidadDecimales { get; set; }

    public bool Estado { get; set; }
}