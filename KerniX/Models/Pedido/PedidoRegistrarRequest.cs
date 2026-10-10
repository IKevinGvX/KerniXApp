
using System.ComponentModel.DataAnnotations;

namespace KerniX.Models.Pedido;

public sealed class PedidoRegistrarRequest
{
    [MaxLength(30)]
    public string? NumeroPedido { get; set; }

    [Range(1, int.MaxValue)]
    public int IdCliente { get; set; }

    public long? IdCotizacion { get; set; }

    public DateTime? FechaEntregaEstimada { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }

    [Required]
    [MinLength(1)]
    public List<PedidoDetalleRequest> Detalles { get; set; }
        = new();
}

public sealed class PedidoDetalleRequest
{
    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Range(
        typeof(decimal),
        "0.0001",
        "99999999999999.9999"
    )]
    public decimal CantidadSolicitada { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999999999.9999"
    )]
    public decimal PrecioUnitario { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999999999.9999"
    )]
    public decimal Descuento { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999999999.9999"
    )]
    public decimal Impuesto { get; set; }
}