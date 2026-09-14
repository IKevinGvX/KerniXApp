namespace KerniX.Models.Ventas.Pedido;

public sealed class PedidoCrearRequest
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public long? IdCotizacion { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public string? Observacion { get; set; }

    public List<PedidoDetalleRequest> Detalles { get; set; }
        = new();
}