namespace KerniX.Models.Ventas.Cotizacion;

public sealed class CotizacionResponse
{
    public long IdCotizacion { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public string NumeroCotizacion { get; set; }
        = string.Empty;

    public DateTime FechaCotizacion { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public string Vendedor { get; set; }
        = string.Empty;
}