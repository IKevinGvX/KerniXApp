using KerniX.Models.Configuracion.Usuarios;
using KerniX.Security;
using KerniX.Services.Configuracion.Usuarios;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion.Usuarios;

public sealed class MonitoreoModel : PageModel
{
    private readonly IUsuarioAdminDbService _usuarioService;
    private readonly ILogger<MonitoreoModel> _logger;

    public MonitoreoModel(
        IUsuarioAdminDbService usuarioService,
        ILogger<MonitoreoModel> logger)
    {
        _usuarioService =
            usuarioService;

        _logger =
            logger;
    }


    public int IdUsuarioSesion { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }


    [BindProperty(SupportsGet = true)]
    public int? IdUsuario { get; set; }


    public const int TamanoPagina = 20;


    [BindProperty(SupportsGet = true)]
    public int PaginaSesiones { get; set; } = 1;


    [BindProperty(SupportsGet = true)]
    public int PaginaAccesos { get; set; } = 1;


    [BindProperty(SupportsGet = true)]
    public int PaginaAuditoria { get; set; } = 1;


    public IReadOnlyList<UsuarioAdminListaItem> Usuarios { get; private set; } =
        Array.Empty<UsuarioAdminListaItem>();


    public UsuarioMonitoreoResultado Monitoreo { get; private set; } =
        new();


    public IReadOnlyList<UsuarioSesionMonitoreoItem> SesionesPagina =>
        Monitoreo.Sesiones
            .Skip(
                (PaginaSesiones - 1) *
                TamanoPagina
            )
            .Take(
                TamanoPagina
            )
            .ToList();


    public IReadOnlyList<UsuarioHistorialAccesoItem> AccesosPagina =>
        Monitoreo.HistorialAcceso
            .Skip(
                (PaginaAccesos - 1) *
                TamanoPagina
            )
            .Take(
                TamanoPagina
            )
            .ToList();


    public IReadOnlyList<UsuarioAuditoriaMonitoreoItem> AuditoriaPagina =>
        Monitoreo.Auditoria
            .Skip(
                (PaginaAuditoria - 1) *
                TamanoPagina
            )
            .Take(
                TamanoPagina
            )
            .ToList();


    public int TotalPaginasSesiones =>
        TotalPaginas(
            Monitoreo.Sesiones.Count
        );


    public int TotalPaginasAccesos =>
        TotalPaginas(
            Monitoreo.HistorialAcceso.Count
        );


    public int TotalPaginasAuditoria =>
        TotalPaginas(
            Monitoreo.Auditoria.Count
        );


    [TempData]
    public string? Mensaje { get; set; }

    [TempData]
    public string? Error { get; set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await CargarUsuariosAsync(
                cancellationToken
            );


            if (
                !IdUsuario.HasValue &&
                Usuarios.Count > 0
            )
            {
                IdUsuario =
                    Usuarios[0].IdUsuario;
            }


            if (
                IdUsuario.HasValue &&
                IdUsuario.Value > 0
            )
            {
                Monitoreo =
                    await _usuarioService
                        .ObtenerMonitoreoAsync(
                            IdUsuario.Value,
                            IdEmpresa,
                            cancellationToken
                        );


                if (Monitoreo.Usuario is null)
                {
                    Error =
                        "No se encontró el usuario seleccionado.";
                }

                NormalizarPaginas();
            }


            _logger.LogInformation(
                "[USUARIOS MONITOREO] GET. Admin={Admin} Empresa={Empresa} Usuario={Usuario} TotalUsuarios={Total}",
                IdUsuarioSesion,
                IdEmpresa,
                IdUsuario,
                Usuarios.Count
            );


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS MONITOREO] Error cargando panel."
            );


            Error =
                ex.Message;


            return Page();
        }
    }


    // =========================================================
    // CERRAR SESIONES DESDE MONITOREO
    // =========================================================

    public async Task<IActionResult> OnPostRevocarSesionesAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _usuarioService
                .RevocarSesionesAsync(
                    idUsuario,
                    cancellationToken
                );


            Mensaje =
                "Sesiones activas revocadas correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS MONITOREO] Error revocando sesiones. Usuario={Usuario}",
                idUsuario
            );


            Error =
                "No se pudieron revocar las sesiones.";
        }


        return RedirectToPage(
            "./Monitoreo",
            new
            {
                IdUsuario =
                    idUsuario,

                PaginaSesiones,
                PaginaAccesos,
                PaginaAuditoria
            }
        );
    }


    // =========================================================
    // CARGAR CATÁLOGO
    // =========================================================

    private async Task CargarUsuariosAsync(
        CancellationToken cancellationToken)
    {
        UsuarioAdminResultado resultado =
            await _usuarioService
                .ListarAsync(
                    new UsuarioAdminFiltro
                    {
                        IdEmpresa =
                            IdEmpresa,

                        IdSucursal =
                            null,

                        Estado =
                            null,

                        IdRol =
                            null,

                        Buscar =
                            null,

                        Pagina =
                            1,

                        TamanoPagina =
                            100
                    },
                    cancellationToken
                );


        Usuarios =
            resultado.Items
                .OrderBy(
                    x =>
                        x.Empleado
                )
                .ThenBy(
                    x =>
                        x.NombreUsuario
                )
                .ToList();
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    private bool CargarSesion()
    {
        IdUsuarioSesion =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );


        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );


        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );


        if (IdUsuarioSesion <= 0)
        {
            return false;
        }


        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }


        return true;
    }


    private int ObtenerEnteroSesion(
        string clave)
    {
        int? valor =
            HttpContext.Session.GetInt32(
                clave
            );


        if (valor.HasValue)
        {
            return valor.Value;
        }


        string? texto =
            HttpContext.Session.GetString(
                clave
            );


        return int.TryParse(
            texto,
            out int resultado
        )
            ? resultado
            : 0;
    }


    // =========================================================
    // PAGINACIÓN
    // =========================================================

    private void NormalizarPaginas()
    {
        PaginaSesiones =
            LimitarPagina(
                PaginaSesiones,
                TotalPaginasSesiones
            );

        PaginaAccesos =
            LimitarPagina(
                PaginaAccesos,
                TotalPaginasAccesos
            );

        PaginaAuditoria =
            LimitarPagina(
                PaginaAuditoria,
                TotalPaginasAuditoria
            );
    }


    private static int LimitarPagina(
        int pagina,
        int totalPaginas)
    {
        int maximo =
            Math.Max(
                1,
                totalPaginas
            );

        if (pagina < 1)
        {
            return 1;
        }

        if (pagina > maximo)
        {
            return maximo;
        }

        return pagina;
    }


    private static int TotalPaginas(
        int totalRegistros)
    {
        if (totalRegistros <= 0)
        {
            return 1;
        }

        return
            (int)Math.Ceiling(
                totalRegistros /
                (double)TamanoPagina
            );
    }


    public int RegistroDesde(
        int pagina,
        int totalRegistros)
    {
        if (totalRegistros <= 0)
        {
            return 0;
        }

        return
            ((pagina - 1) * TamanoPagina)
            + 1;
    }


    public int RegistroHasta(
        int pagina,
        int totalRegistros)
    {
        if (totalRegistros <= 0)
        {
            return 0;
        }

        return
            Math.Min(
                pagina * TamanoPagina,
                totalRegistros
            );
    }


    // =========================================================
    // UI
    // =========================================================

    public string EquipoEstadoTexto =>
        Monitoreo.Equipo is null
            ? "Sin equipo"
            : string.IsNullOrWhiteSpace(
                Monitoreo.Equipo.Estado
            )
                ? "Registrado"
                : Monitoreo.Equipo.Estado;


    public string AgenteEstadoTexto
    {
        get
        {
            if (Monitoreo.Agente is null)
            {
                return "Sin agente";
            }

            if (
                Monitoreo.Agente.UltimaComunicacion.HasValue &&
                Monitoreo.Agente.UltimaComunicacion.Value >=
                DateTime.Now.AddMinutes(-10)
            )
            {
                return "En línea";
            }

            return string.IsNullOrWhiteSpace(
                Monitoreo.Agente.Estado
            )
                ? "Sin comunicación reciente"
                : Monitoreo.Agente.Estado;
        }
    }


    public string MetricaTexto(
        decimal? valor,
        string sufijo = "")
    {
        return valor.HasValue
            ? $"{valor.Value:N2}{sufijo}"
            : "-";
    }


    public string NivelMetrica(
        decimal? valor,
        decimal advertencia,
        decimal critico)
    {
        if (!valor.HasValue)
        {
            return "normal";
        }

        if (valor.Value >= critico)
        {
            return "critical";
        }

        if (valor.Value >= advertencia)
        {
            return "warning";
        }

        return "normal";
    }


    public string EstadoCuenta =>
        Monitoreo.Usuario is null
            ? "-"
            : Monitoreo.Usuario.Estado
                ? "Activo"
                : "Inactivo";


    public string EstadoRiesgo
    {
        get
        {
            if (Monitoreo.Usuario is null)
            {
                return "Sin datos";
            }


            if (
                Monitoreo.Usuario.BloqueadoHasta.HasValue &&
                Monitoreo.Usuario.BloqueadoHasta.Value > DateTime.Now
            )
            {
                return "Bloqueado";
            }


            if (
                Monitoreo.Usuario.IntentosFallidos >= 5 ||
                Monitoreo.AccesosFallidos >= 5
            )
            {
                return "Atención alta";
            }


            if (
                Monitoreo.Usuario.IntentosFallidos > 0 ||
                Monitoreo.AccesosFallidos > 0
            )
            {
                return "Revisar";
            }


            return "Normal";
        }
    }


    public string FechaTexto(
        DateTime? fecha)
    {
        return fecha.HasValue
            ? fecha.Value.ToString(
                "dd/MM/yyyy HH:mm:ss"
            )
            : "-";
    }


    public string ExitoTexto(
        bool? exitoso)
    {
        return exitoso switch
        {
            true => "Exitoso",
            false => "Fallido",
            _ => "Sin dato"
        };
    }
}
