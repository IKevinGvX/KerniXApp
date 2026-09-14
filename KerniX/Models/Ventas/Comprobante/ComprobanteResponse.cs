namespace KerniX.Models.Ventas.Comprobante;

public sealed class ComprobanteResponse
{
    public long IdComprobante { get; set; }

    public long IdVenta { get; set; }

    public string TipoComprobante { get; set; }
        = string.Empty;

    public string Serie { get; set; }
        = string.Empty;

    public string Numero { get; set; }
        = string.Empty;

    public DateTime FechaEmision { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string? EstadoSunat { get; set; }

    public string? CodigoHash { get; set; }

    public string? RutaXml { get; set; }

    public string? RutaPdf { get; set; }

    public string NumeroCompleto
    {
        get
        {
            return $"{Serie}-{Numero}";
        }
    }
}