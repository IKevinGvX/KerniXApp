namespace KerniX.Api.Models.Configuracion.Dashboard;

public sealed class ConfiguracionValidacionResponse
{
    public List<ConfiguracionObservacionResponse> Observaciones { get; set; }
        = new();

    public ConfiguracionEstadoResponse Estado { get; set; }
        = new();
}