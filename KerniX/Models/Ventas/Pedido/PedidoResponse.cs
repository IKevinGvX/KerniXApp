namespace KerniX.Models.Ventas.Pedido;

public sealed class PedidoResponse
{
    public long IdPedido { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public long? IdCotizacion { get; set; }

    public string NumeroPedido { get; set; }
        = string.Empty;

    public DateTime FechaPedido { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public string Cliente { get; set; }
        = string.Empty;
}