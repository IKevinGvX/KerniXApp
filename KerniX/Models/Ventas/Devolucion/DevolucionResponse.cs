namespace KerniX.Models.Ventas.Devolucion;

public sealed class DevolucionResponse
{
    public long IdDevolucion { get; set; }

    public long IdVenta { get; set; }

    public int IdEmpleado { get; set; }

    public DateTime FechaDevolucion { get; set; }

    public string Motivo { get; set; }
        = string.Empty;

    public decimal TotalDevuelto { get; set; }

    public string Estado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }

    public string NumeroVenta { get; set; }
        = string.Empty;

    public string Cliente { get; set; }
        = string.Empty;
}