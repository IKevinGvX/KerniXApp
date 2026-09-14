namespace KerniX.Api.Models.Configuracion.Dashboard;

public sealed class ConfiguracionEstadoResponse
{
    public string EstadoConfiguracion { get; set; }
        = string.Empty;

    public int TotalObservaciones { get; set; }

    public int Criticas { get; set; }

    public int Altas { get; set; }

    public int Medias { get; set; }
}