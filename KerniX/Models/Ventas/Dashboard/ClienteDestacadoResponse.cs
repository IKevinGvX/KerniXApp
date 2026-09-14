namespace KerniX.Models.Ventas.Dashboard;

public sealed class ClienteDestacadoResponse
{
    public int Posicion { get; set; }

    public string Cliente { get; set; }
        = string.Empty;

    public string Documento { get; set; }
        = string.Empty;

    public decimal TotalComprado { get; set; }

    public int CantidadCompras { get; set; }

    public decimal TicketPromedio { get; set; }

    public decimal PorcentajeParticipacion { get; set; }

    public DateTime? UltimaCompra { get; set; }
}