namespace KerniX.Api.Models.Configuracion.ConfiguracionSucursal;

public sealed class ConfiguracionSucursalActualizarRequest
{
    public string? Valor { get; set; }

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }
}