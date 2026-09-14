namespace KerniX.Api.Models.Configuracion.Dashboard;

public sealed class ConfiguracionDiagnosticoResponse
{
    public int IdEmpresa { get; set; }

    public int IdSucursal { get; set; }

    public bool EmpresaExiste { get; set; }

    public bool SucursalExiste { get; set; }

    public int ParametrosActivos { get; set; }

    public int ConfiguracionesSucursalActivas { get; set; }

    public int MonedasActivas { get; set; }

    public int ImpuestosVigentes { get; set; }

    public int SeriesActivas { get; set; }

    public int PlanesMantenimientoActivos { get; set; }

    public int AlertasPendientes { get; set; }

    public DateTime? UltimaCopiaSeguridad { get; set; }

    public DateTime? UltimoMantenimientoExitoso { get; set; }
}