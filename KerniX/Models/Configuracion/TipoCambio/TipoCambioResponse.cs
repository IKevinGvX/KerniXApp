namespace KerniX.Api.Models.Configuracion.TipoCambio;

public sealed class TipoCambioResponse
{
    public long IdTipoCambio { get; set; }

    public int IdMonedaOrigen { get; set; }

    public string MonedaOrigenCodigo { get; set; }
        = string.Empty;

    public string? MonedaOrigenNombre { get; set; }

    public int IdMonedaDestino { get; set; }

    public string MonedaDestinoCodigo { get; set; }
        = string.Empty;

    public string? MonedaDestinoNombre { get; set; }

    public DateTime Fecha { get; set; }

    public decimal ValorCompra { get; set; }

    public decimal ValorVenta { get; set; }

    public string? Fuente { get; set; }
}