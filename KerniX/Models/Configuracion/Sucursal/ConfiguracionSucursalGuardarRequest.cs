namespace KerniX.Api.Models.Configuracion.ConfiguracionSucursal;

public sealed class ConfiguracionSucursalGuardarRequest
{
    public int IdSucursal { get; set; }

    public string Codigo { get; set; }
        = string.Empty;

    public string? Valor { get; set; }

    public string? Descripcion { get; set; }
}