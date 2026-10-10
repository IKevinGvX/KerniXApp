namespace KerniX.Models.Inventario.EntradaStock;

public sealed class EntradaStockResumen
{
    public long TotalEntradas { get; set; }

    public decimal CantidadIngresada { get; set; }

    public int EntradasHoy { get; set; }

    public decimal CantidadHoy { get; set; }
}
