namespace KerniX.Api.Models.Configuracion.SerieComprobante;

public sealed class SerieComprobanteResponse
{
    public int IdSerieComprobante { get; set; }

    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public string TipoComprobante { get; set; }
        = string.Empty;

    public string Serie { get; set; }
        = string.Empty;

    public long CorrelativoActual { get; set; }

    public bool Estado { get; set; }
}