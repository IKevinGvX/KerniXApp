namespace KerniX.Models.Cotizacion;

public sealed class CotizacionDetalleResponse
{
    public long IdCotizacion { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public string NumeroCotizacion { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public string Cliente { get; set; } = string.Empty;

    public string? TipoDocumento { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public List<CotizacionDetalleProducto> Productos { get; set; } = new();
}

public sealed class CotizacionDetalleProducto
{
    public long IdDetalleCotizacion { get; set; }

    public long IdCotizacion { get; set; }

    public int IdProducto { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total =>
        Subtotal - Descuento + Impuesto;
}