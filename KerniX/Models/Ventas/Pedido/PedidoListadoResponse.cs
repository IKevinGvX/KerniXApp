namespace KerniX.Models.Ventas.Pedido;

public sealed class PedidoListadoResponse
{
    public long IdPedido { get; set; }

    public string NumeroPedido { get; set; }
        = string.Empty;

    public DateTime FechaPedido { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public decimal Total { get; set; }

    public string Estado { get; set; }
        = string.Empty;
}