namespace KerniX.Models.Proveedores;

public sealed class ProveedorItem
{
    public int IdProveedor { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public sealed class ProveedorListado
{
    public List<ProveedorItem> Items { get; set; } = new();
    public int Total { get; set; }
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public int TotalFiltrado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 12;
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)TotalFiltrado / TamanoPagina));
}
