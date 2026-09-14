namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaCategoriaResponse
{
    public int IdCategoria { get; set; }

    public string Categoria { get; set; }
        = string.Empty;

    public decimal TotalVenta { get; set; }

    public decimal Porcentaje { get; set; }

    public int CantidadVendida { get; set; }
}