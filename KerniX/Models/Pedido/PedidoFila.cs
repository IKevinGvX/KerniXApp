namespace KerniX.Models.Pedido;

public sealed class PedidoFila
{
    public long IdPedido { get; init; }

    public string Numero { get; init; }
        = string.Empty;

    public string Cliente { get; init; }
        = string.Empty;

    public DateTime Fecha { get; init; }

    public decimal Total { get; init; }

    public string Estado { get; init; }
        = string.Empty;

    public decimal CantidadSolicitada { get; init; }

    public decimal CantidadAtendida { get; init; }

    public int VentasVinculadas { get; init; }

    public long? IdVenta { get; init; }

    public long? IdCotizacion { get; init; }
}