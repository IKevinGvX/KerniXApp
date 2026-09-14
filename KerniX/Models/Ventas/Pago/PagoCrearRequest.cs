namespace KerniX.Models.Ventas.Pago;

public sealed class PagoCrearRequest
{
    public long IdVenta { get; set; }

    public int IdEmpleado { get; set; }

    public string MetodoPago { get; set; }
        = string.Empty;

    public string Moneda { get; set; }
        = "PEN";

    public decimal Monto { get; set; }

    public string? NumeroOperacion { get; set; }
}