namespace KerniX.Models.Ventas.Venta;

public sealed class VentaResponse
{
    public long IdVenta { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public long? IdPedido { get; set; }

    public string NumeroVenta { get; set; }
        = string.Empty;

    public DateTime FechaVenta { get; set; }

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

    public string Sucursal { get; set; }
        = string.Empty;

    public IReadOnlyList<VentaDetalleResponse> Detalles { get; set; }
        = Array.Empty<VentaDetalleResponse>();
}