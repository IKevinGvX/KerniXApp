namespace KerniX.Models.Ventas.Venta;

public sealed class VentaCrearRequest
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public int IdAlmacen { get; set; }

    public long? IdPedido { get; set; }

    public string NumeroVenta { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public List<VentaDetalleCrearRequest> Detalles { get; set; }
        = new();
}