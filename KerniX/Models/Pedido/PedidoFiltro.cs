namespace KerniX.Models.Pedido;

public sealed class PedidoFiltro
{
    public int IdEmpresa { get; init; }

    public int? IdSucursal { get; init; }

    public string? Buscar { get; init; }

    public string? Estado { get; init; }
}