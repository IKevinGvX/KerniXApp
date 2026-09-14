namespace KerniX.Models.Ventas.Pago;

public sealed class PagoResponse
{
    public long IdPago { get; set; }

    public long IdVenta { get; set; }

    public int IdEmpleado { get; set; }

    public string MetodoPago { get; set; }
        = string.Empty;

    public string Moneda { get; set; }
        = string.Empty;

    public decimal Monto { get; set; }

    public string? NumeroOperacion { get; set; }

    public DateTime FechaPago { get; set; }

    public string Estado { get; set; }
        = string.Empty;

    public decimal TotalVenta { get; set; }

    public decimal TotalPagado { get; set; }

    public decimal SaldoPendiente { get; set; }
}