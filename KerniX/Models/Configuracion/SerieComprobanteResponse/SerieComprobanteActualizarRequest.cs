namespace KerniX.Api.Models.Configuracion.SerieComprobante;

public sealed class SerieComprobanteActualizarRequest
{
    public string TipoComprobante { get; set; }
        = string.Empty;

    public string Serie { get; set; }
        = string.Empty;

    public bool Estado { get; set; }
}