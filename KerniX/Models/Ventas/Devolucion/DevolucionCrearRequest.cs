namespace KerniX.Models.Ventas.Devolucion;

public sealed class DevolucionCrearRequest
{
    public long IdVenta { get; set; }

    public int IdEmpleado { get; set; }

    public int IdAlmacen { get; set; }

    public string Motivo { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public List<DevolucionDetalleRequest> Detalles { get; set; }
        = new();
}