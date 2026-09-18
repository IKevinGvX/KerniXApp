namespace KerniX.Models.Cotizacion;

public sealed class CotizacionListaItem
{
    public long IdCotizacion { get; set; }

    public string NumeroCotizacion { get; set; }
        = string.Empty;

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int IdCliente { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public string TipoDocumento { get; set; }
        = string.Empty;

    public string NumeroDocumento { get; set; }
        = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdEmpleado { get; set; }
}