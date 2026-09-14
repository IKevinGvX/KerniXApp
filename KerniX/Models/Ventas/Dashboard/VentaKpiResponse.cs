namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaKpiResponse
{
    public decimal VentasMes { get; set; }

    public decimal VentasHoy { get; set; }

    public decimal TicketPromedio { get; set; }

    public int TotalVentasMes { get; set; }

    public int TotalVentasHoy { get; set; }

    public int ClientesActivos { get; set; }

    public decimal VentasAnuales { get; set; }

    public decimal VariacionMes { get; set; }

    public decimal VariacionHoy { get; set; }

    public decimal VariacionTicket { get; set; }
}