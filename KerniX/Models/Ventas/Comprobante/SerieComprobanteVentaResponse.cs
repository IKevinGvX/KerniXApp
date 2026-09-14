namespace KerniX.Models.Ventas.Comprobante;

public sealed class SerieComprobanteVentaResponse
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

    public long SiguienteCorrelativo
    {
        get
        {
            return CorrelativoActual + 1;
        }
    }

    public string NumeroSiguiente
    {
        get
        {
            return SiguienteCorrelativo
                .ToString("D8");
        }
    }

    public string NumeroCompleto
    {
        get
        {
            return $"{Serie}-{NumeroSiguiente}";
        }
    }
}