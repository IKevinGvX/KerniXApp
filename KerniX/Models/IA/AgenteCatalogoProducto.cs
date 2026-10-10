namespace KerniX.Models.IA;

/// <summary>
/// Fotografía de consulta del catálogo. No reserva stock ni fija precios.
/// </summary>
public sealed class AgenteCatalogoProducto
{
    public int IdProducto { get; init; }
    public string Codigo { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Descripcion { get; init; } = "";
    public decimal? PrecioVenta { get; init; }
    public decimal? StockDisponible { get; init; }
}
