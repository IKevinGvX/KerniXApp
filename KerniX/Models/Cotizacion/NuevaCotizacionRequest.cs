namespace KerniX.Models.Cotizacion;

public sealed class NuevaCotizacionRequest
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public decimal Descuento { get; set; }

    public string? Observacion { get; set; }

    public List<NuevaCotizacionDetalleRequest>
        Detalles
    { get; set; }
        = new();
}