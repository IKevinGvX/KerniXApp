namespace KerniX.Api.Models.Configuracion.TipoCambio;

public sealed class TipoCambioCrearRequest
{
    public int IdMonedaOrigen { get; set; }

    public int IdMonedaDestino { get; set; }

    public DateTime Fecha { get; set; }

    public decimal ValorCompra { get; set; }

    public decimal ValorVenta { get; set; }

    public string? Fuente { get; set; }
}