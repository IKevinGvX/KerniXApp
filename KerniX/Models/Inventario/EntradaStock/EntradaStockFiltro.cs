namespace KerniX.Models.Inventario.EntradaStock;

public sealed class EntradaStockFiltro
{
    public int IdEmpresa { get; set; }

    public int? IdSucursal { get; set; }

    public int? IdAlmacen { get; set; }

    public int? IdProducto { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Buscar { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;
}
