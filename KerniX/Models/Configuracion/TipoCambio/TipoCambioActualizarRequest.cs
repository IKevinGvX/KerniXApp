namespace KerniX.Api.Models.Configuracion.TipoCambio;

public sealed class TipoCambioActualizarRequest
{
    public DateTime Fecha { get; set; }

    public decimal ValorCompra { get; set; }

    public decimal ValorVenta { get; set; }

    public string? Fuente { get; set; }
}