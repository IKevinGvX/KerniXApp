namespace KerniX.Models.Ventas.Cotizacion;

public sealed class CotizacionCrearRequest
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string? Observacion { get; set; }

    public List<CotizacionDetalleRequest> Detalles { get; set; }
        = new();
}