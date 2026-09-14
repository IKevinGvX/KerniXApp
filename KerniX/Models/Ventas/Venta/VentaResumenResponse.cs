namespace KerniX.Models.Ventas.Venta;

public sealed class VentaResumenResponse
{
    public decimal VentasMes { get; set; }

    public decimal VentasHoy { get; set; }

    public decimal TicketPromedio { get; set; }

    public int ClientesActivos { get; set; }

    public decimal VentasAnuales { get; set; }
}