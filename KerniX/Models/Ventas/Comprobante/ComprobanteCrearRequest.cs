namespace KerniX.Models.Ventas.Comprobante;

public sealed class ComprobanteCrearRequest
{
    public long IdVenta { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public string TipoComprobante { get; set; }
        = string.Empty;

    public string? EstadoSunat { get; set; }

    public string? CodigoHash { get; set; }

    public string? RutaXml { get; set; }

    public string? RutaPdf { get; set; }
}