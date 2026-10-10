using System;
using System.Collections.Generic;

namespace KerniX.Models.Configuracion.Usuarios;

public sealed class UsuarioAdminListaItem
{
    public int IdUsuario { get; set; }
    public int? IdEmpleado { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Empleado { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public bool RequiereCambioClave { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public string Roles { get; set; } = string.Empty;

    public bool EstaBloqueado =>
        BloqueadoHasta.HasValue &&
        BloqueadoHasta.Value > DateTime.Now;
}

public sealed class RolAdminItem
{
    public int IdRol { get; set; }
    public int? IdEmpresa { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Estado { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalPermisos { get; set; }
}

public sealed class UsuarioAdminFiltro
{
    public int IdEmpresa { get; set; }
    public int? IdSucursal { get; set; }
    public bool? Estado { get; set; }
    public int? IdRol { get; set; }
    public string? Buscar { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}

public sealed class UsuarioAdminResultado
{
    public List<UsuarioAdminListaItem> Items { get; set; } = new();
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalRegistros /
                (double)TamanoPagina
            );
}

public sealed class UsuarioAdminResumen
{
    public int TotalUsuarios { get; set; }
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public int Bloqueados { get; set; }
    public int RequierenCambioClave { get; set; }
}

public sealed class EmpleadoUsuarioDisponibleItem
{
    public int IdEmpleado { get; set; }
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }

    public string Empleado { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
}

public sealed class UsuarioCrearAdminRequest
{
    public int IdEmpleado { get; set; }
    public int IdRol { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordTemporal { get; set; } = string.Empty;

    public bool RequiereCambioClave { get; set; } = true;
    public bool Estado { get; set; } = true;

    public int IdUsuarioRegistro { get; set; }
}

public sealed class UsuarioAdminEditarDetalle
{
    public int IdUsuario { get; set; }
    public int IdEmpleado { get; set; }
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    public string Empleado { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;

    public bool Estado { get; set; }
    public bool RequiereCambioClave { get; set; }

    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    public int? IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
}

public sealed class UsuarioAdminActualizarRequest
{
    public int IdUsuario { get; set; }
    public int IdEmpleado { get; set; }
    public int IdRol { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    public bool Estado { get; set; }
    public bool RequiereCambioClave { get; set; }

    public int IdUsuarioModifica { get; set; }
}

public sealed class UsuarioSesionMonitoreoItem
{
    public long? IdSesion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Dispositivo { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public sealed class UsuarioHistorialAccesoItem
{
    public long? IdHistorialAcceso { get; set; }
    public DateTime? Fecha { get; set; }
    public bool? Exitoso { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

public sealed class UsuarioAuditoriaMonitoreoItem
{
    public long? IdAuditoria { get; set; }
    public int IdUsuarioActor { get; set; }
    public string UsuarioActor { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string IdRegistro { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string ValorAnterior { get; set; } = string.Empty;
    public string ValorNuevo { get; set; } = string.Empty;
    public DateTime? FechaRegistro { get; set; }
}



public sealed class UsuarioEquipoMonitoreoItem
{
    public int IdMaquina { get; set; }

    public string CodigoEquipo { get; set; } = string.Empty;
    public string NombreEquipo { get; set; } = string.Empty;
    public string NumeroSerie { get; set; } = string.Empty;

    public string DireccionMac { get; set; } = string.Empty;
    public string DireccionIpActual { get; set; } = string.Empty;

    public string SistemaOperativo { get; set; } = string.Empty;
    public string VersionSistemaOperativo { get; set; } = string.Empty;
    public string Arquitectura { get; set; } = string.Empty;

    public string Fabricante { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaRegistro { get; set; }
    public DateTime? UltimaComunicacion { get; set; }
}


public sealed class UsuarioAgenteMonitoreoItem
{
    public int IdAgente { get; set; }
    public int IdMaquina { get; set; }

    public string CodigoAgente { get; set; } = string.Empty;
    public string VersionAgente { get; set; } = string.Empty;

    public int? IntervaloEnvioSegundos { get; set; }

    public DateTime? FechaInstalacion { get; set; }
    public DateTime? UltimaComunicacion { get; set; }

    public string Estado { get; set; } = string.Empty;
}


public sealed class UsuarioMetricaMonitoreoItem
{
    public long? IdMetrica { get; set; }
    public int IdMaquina { get; set; }

    public decimal? PorcentajeCpu { get; set; }
    public decimal? PorcentajeRam { get; set; }
    public decimal? PorcentajeDisco { get; set; }

    public decimal? EspacioLibreGb { get; set; }
    public decimal? TemperaturaCpu { get; set; }
    public decimal? LatenciaMs { get; set; }

    public string EstadoRed { get; set; } = string.Empty;

    public DateTime? FechaHora { get; set; }
}

public sealed class UsuarioMonitoreoResultado
{
    public UsuarioAdminEditarDetalle? Usuario { get; set; }

    public UsuarioEquipoMonitoreoItem? Equipo { get; set; }

    public UsuarioAgenteMonitoreoItem? Agente { get; set; }

    public UsuarioMetricaMonitoreoItem? MetricaActual { get; set; }

    public List<UsuarioSesionMonitoreoItem> Sesiones { get; set; } =
        new();

    public List<UsuarioHistorialAccesoItem> HistorialAcceso { get; set; } =
        new();

    public List<UsuarioAuditoriaMonitoreoItem> Auditoria { get; set; } =
        new();

    public int TotalSesiones =>
        Sesiones.Count;

    public int SesionesActivas =>
        Sesiones.Count(
            x =>
                string.Equals(
                    x.Estado,
                    "activa",
                    StringComparison.OrdinalIgnoreCase
                )
        );

    public int SesionesRevocadas =>
        Sesiones.Count(
            x =>
                string.Equals(
                    x.Estado,
                    "revocada",
                    StringComparison.OrdinalIgnoreCase
                )
        );

    public int AccesosExitosos =>
        HistorialAcceso.Count(
            x =>
                x.Exitoso == true
        );

    public int AccesosFallidos =>
        HistorialAcceso.Count(
            x =>
                x.Exitoso == false
        );
}

