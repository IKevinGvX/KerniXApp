namespace KerniX.Api.Models.Configuracion.ParametroSistema;

public sealed class ParametroSistemaActualizarValorRequest
{
    public int IdEmpresa { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string? Valor { get; set; }
}