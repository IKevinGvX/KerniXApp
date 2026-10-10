public sealed class ComprobanteDetalleViewModel
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Unidad { get; set; } = string.Empty;

    public string AbreviaturaUnidad { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal BaseImponible { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }
}