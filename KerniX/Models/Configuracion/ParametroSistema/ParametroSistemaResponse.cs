namespace KerniX.Api.Models.Configuracion.ParametroSistema;

public sealed class ParametroSistemaResponse
{
    public int IdParametro { get; set; }

    public int IdEmpresa { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string Nombre { get; set; }
        = string.Empty;

    public string? Valor { get; set; }

    public string TipoDato { get; set; }
        = string.Empty;

    public string? Categoria { get; set; }

    public string? Descripcion { get; set; }

    public bool Editable { get; set; }

    public bool Estado { get; set; }
}