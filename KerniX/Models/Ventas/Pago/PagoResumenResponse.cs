namespace KerniX.Models.Ventas.Pago;

public sealed class PagoResumenResponse
{
    public long IdVenta { get; set; }

    public decimal TotalVenta { get; set; }

    public decimal TotalPagado { get; set; }

    public decimal SaldoPendiente { get; set; }

    public bool PagadoCompleto
    {
        get
        {
            return SaldoPendiente <= 0;
        }
    }
}