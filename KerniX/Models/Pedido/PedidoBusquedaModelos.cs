namespace KerniX.Models.Pedido;

public sealed class PedidoClienteBusqueda
{
    public int IdCliente { get; init; }
    public string Nombre { get; init; } = string.Empty;
}
public sealed class PedidoProductoBusqueda
{
    public int IdProducto { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public decimal? PrecioUnitario { get; init; }
}
public sealed class PedidoCotizacionBusqueda
{
    public long IdCotizacion { get; init; }
}