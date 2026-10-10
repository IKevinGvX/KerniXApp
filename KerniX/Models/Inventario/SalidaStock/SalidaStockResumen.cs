namespace KerniX.Models.Inventario.SalidaStock;

public sealed class SalidaStockResumen
{
    public long TotalSalidas { get; set; }

    public decimal CantidadRetirada { get; set; }

    public int SalidasHoy { get; set; }

    public decimal CantidadHoy { get; set; }
}
