namespace KerniX.Models.Ventas.Devolucion;

public sealed class DevolucionDetalleRequest
{
    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public string? Motivo { get; set; }
}