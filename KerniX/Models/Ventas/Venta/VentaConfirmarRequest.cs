namespace KerniX.Models.Ventas.Venta;

public sealed class VentaConfirmarRequest
{
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }
    public int IdAlmacen { get; set; }
    public long? IdPedido { get; set; }
    public int IdCliente { get; set; }

    public string? MetodoPago { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Observacion { get; set; }
    public string? NumeroOperacion { get; set; }
    public decimal MontoRecibido { get; set; }

    public List<VentaConfirmarDetalleRequest> Detalles { get; set; } = new();

    public sealed class VentaConfirmarDetalleRequest
    {
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal Descuento { get; set; }
    }
}
