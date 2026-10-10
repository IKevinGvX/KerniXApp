using KerniX.Models.Configuracion.Usuarios;
using KerniX.Security;
using KerniX.Services.Configuracion.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Configuracion.Usuarios;

public sealed class IndexModel : PageModel
{
    private readonly IUsuarioAdminDbService _usuarioService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUsuarioAdminDbService usuarioService,
        ILogger<IndexModel> logger)
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
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdRol { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int TamanoPagina { get; set; } = 20;


    public UsuarioAdminResultado Resultado { get; private set; } =
        new();

    public UsuarioAdminResumen Resumen { get; private set; } =
        new();

    public IReadOnlyList<RolAdminItem> Roles { get; private set; } =
        Array.Empty<RolAdminItem>();


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
            await CargarPantallaAsync(
                cancellationToken
            );

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error cargando panel."
            );

            Error =
                "No fue posible cargar la gestión de usuarios.";

            return Page();
        }
    }


    // =========================================================
    // ESTADO / ELIMINACIÓN LÓGICA
    // =========================================================

    public async Task<IActionResult> OnPostCambiarEstadoAsync(
        int idUsuario,
        bool nuevoEstado,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[USUARIOS ADMIN] POST CambiarEstado. Usuario={Usuario} NuevoEstado={Estado}",
            idUsuario,
            nuevoEstado
        );

        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        try
        {
            await _usuarioService
                .CambiarEstadoAsync(
                    idUsuario,
                    nuevoEstado,
                    IdUsuarioSesion,
                    cancellationToken
                );

            Mensaje =
                nuevoEstado
                    ? "Usuario activado correctamente."
                    : "Acceso del usuario desactivado correctamente.";
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIOS ADMIN] Cambiar estado. Usuario={Usuario}",
                idUsuario
            );

            Error =
                ex.Number switch
                {
                    62201 =>
                        "El usuario no existe.",

                    62202 =>
                        "No puedes desactivar tu propio usuario.",

                    _ =>
                        $"No se pudo cambiar el estado. SQL {ex.Number}: {ex.Message}"
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error cambiando estado Usuario={Usuario}",
                idUsuario
            );

            Error =
                "No se pudo cambiar el estado del usuario.";
        }

        return Volver();
    }


    public async Task<IActionResult> OnPostEliminarAsync(
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
            if (idUsuario == IdUsuarioSesion)
            {
                Error =
                    "No puedes eliminar o desactivar tu propia sesión.";

                return RedirectToPage(
                    "./Index"
                );
            }

            await _usuarioService
                .CambiarEstadoAsync(
                    idUsuario,
                    false,
                    IdUsuarioSesion,
                    cancellationToken
                );

            Mensaje =
                "Acceso eliminado lógicamente. El historial del usuario se conserva.";
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIOS ADMIN] Eliminar acceso. Usuario={Usuario}",
                idUsuario
            );

            Error =
                ex.Number switch
                {
                    62201 =>
                        "El usuario no existe.",

                    62202 =>
                        "No puedes desactivar tu propio usuario.",

                    _ =>
                        $"No se pudo eliminar el acceso. SQL {ex.Number}: {ex.Message}"
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error eliminando acceso. Usuario={Usuario}",
                idUsuario
            );

            Error =
                "No fue posible eliminar el acceso del usuario.";
        }

        return RedirectToPage(
            "./Index"
        );
    }


    // =========================================================
    // BLOQUEAR
    // =========================================================

    public async Task<IActionResult> OnPostBloquearAsync(
        int idUsuario,
        int minutos,
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
                .BloquearAsync(
                    idUsuario,
                    minutos <= 0 ? 30 : minutos,
                    cancellationToken
                );

            Mensaje =
                $"Usuario bloqueado temporalmente por {(minutos <= 0 ? 30 : minutos)} minutos.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error bloqueando Usuario={Usuario}",
                idUsuario
            );

            Error =
                "No fue posible bloquear el usuario.";
        }

        return Volver();
    }


    public async Task<IActionResult> OnPostDesbloquearAsync(
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
                .DesbloquearAsync(
                    idUsuario,
                    cancellationToken
                );

            Mensaje =
                "Usuario desbloqueado correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error desbloqueando Usuario={Usuario}",
                idUsuario
            );

            Error =
                "No fue posible desbloquear el usuario.";
        }

        return Volver();
    }


    // =========================================================
    // SESIONES
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
                "Las sesiones activas del usuario fueron revocadas.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error revocando sesiones Usuario={Usuario}",
                idUsuario
            );

            Error =
                "No fue posible revocar las sesiones del usuario.";
        }

        return Volver();
    }


    // =========================================================
    // ASIGNAR ROL
    // =========================================================

    public async Task<IActionResult> OnPostAsignarRolAsync(
        int idUsuario,
        int idRolAsignar,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        if (
            idUsuario <= 0 ||
            idRolAsignar <= 0
        )
        {
            Error =
                "Selecciona un usuario y un rol válido.";

            return RedirectToPage(
                "./Index"
            );
        }

        if (idUsuario == IdUsuarioSesion)
        {
            Error =
                "Por seguridad, la categoría de tu sesión actual se modifica únicamente desde Editar Usuario.";

            return RedirectToPage(
                "./Index"
            );
        }

        try
        {
            await _usuarioService
                .AsignarRolAsync(
                    idUsuario,
                    idRolAsignar,
                    IdUsuarioSesion,
                    cancellationToken
                );

            Mensaje =
                "Rol asignado correctamente.";
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIOS ADMIN] Error asignando rol. Usuario={Usuario} Rol={Rol}",
                idUsuario,
                idRolAsignar
            );

            Error =
                ex.Number switch
                {
                    62402 =>
                        "El usuario no existe.",

                    62403 =>
                        "El rol no existe o está inactivo.",

                    _ =>
                        $"No se pudo asignar el rol. SQL {ex.Number}: {ex.Message}"
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error asignando rol."
            );

            Error =
                "No fue posible asignar el rol.";
        }

        return Volver();
    }


    // =========================================================
    // QUITAR ROL
    // =========================================================

    public async Task<IActionResult> OnPostQuitarRolAsync(
        int idUsuario,
        int idRol,
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
                .QuitarRolAsync(
                    idUsuario,
                    idRol,
                    cancellationToken
                );

            Mensaje =
                "Rol retirado correctamente.";
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIOS ADMIN] Error quitando rol."
            );

            Error =
                ex.Number == 51404
                    ? "La asignación de rol no existe."
                    : $"No se pudo quitar el rol. SQL {ex.Number}: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIOS ADMIN] Error quitando rol."
            );

            Error =
                "No fue posible quitar el rol.";
        }

        return Volver();
    }


    // =========================================================
    // CARGA
    // =========================================================

    private async Task CargarPantallaAsync(
        CancellationToken cancellationToken)
    {
        bool? estadoFiltro =
            Estado?.Trim().ToLowerInvariant()
            switch
            {
                "activo" => true,
                "inactivo" => false,
                _ => null
            };


        UsuarioAdminFiltro filtro =
            new()
            {
                IdEmpresa =
                    IdEmpresa,

                IdSucursal =
                    null,

                Estado =
                    estadoFiltro,

                IdRol =
                    IdRol,

                Buscar =
                    string.IsNullOrWhiteSpace(
                        Buscar
                    )
                        ? null
                        : Buscar.Trim(),

                Pagina =
                    Pagina < 1
                        ? 1
                        : Pagina,

                TamanoPagina =
                    TamanoPagina is < 1 or > 100
                        ? 20
                        : TamanoPagina
            };


        Task<UsuarioAdminResultado> usuariosTask =
            _usuarioService
                .ListarAsync(
                    filtro,
                    cancellationToken
                );

        Task<UsuarioAdminResumen> resumenTask =
            _usuarioService
                .ObtenerResumenAsync(
                    IdEmpresa,
                    null,
                    cancellationToken
                );

        Task<IReadOnlyList<RolAdminItem>> rolesTask =
            _usuarioService
                .ListarRolesAsync(
                    IdEmpresa,
                    cancellationToken
                );


        await Task.WhenAll(
            usuariosTask,
            resumenTask,
            rolesTask
        );


        Resultado =
            await usuariosTask;

        Resumen =
            await resumenTask;

        Roles =
            await rolesTask;
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
    // VOLVER CONSERVANDO FILTROS
    // =========================================================

    private IActionResult Volver()
    {
        return RedirectToPage(
            "./Index",
            new
            {
                Buscar =
                    string.IsNullOrWhiteSpace(Buscar)
                        ? null
                        : Buscar,

                Estado =
                    string.IsNullOrWhiteSpace(Estado)
                        ? null
                        : Estado,

                IdRol,
                Pagina =
                    Pagina < 1
                        ? 1
                        : Pagina,

                TamanoPagina =
                    TamanoPagina is < 1 or > 100
                        ? 20
                        : TamanoPagina
            }
        );
    }


    // =========================================================
    // HELPERS UI
    // =========================================================

    public string EstadoTexto(
        UsuarioAdminListaItem usuario)
    {
        if (!usuario.Estado)
        {
            return "Inactivo";
        }

        if (usuario.EstaBloqueado)
        {
            return "Bloqueado";
        }

        return "Activo";
    }


    public string EstadoClase(
        UsuarioAdminListaItem usuario)
    {
        if (!usuario.Estado)
        {
            return "kx-status-off";
        }

        if (usuario.EstaBloqueado)
        {
            return "kx-status-block";
        }

        return "kx-status-on";
    }


    public string UltimoAccesoTexto(
        UsuarioAdminListaItem usuario)
    {
        return usuario.UltimoAcceso.HasValue
            ? usuario.UltimoAcceso.Value.ToString(
                "dd/MM/yyyy HH:mm"
            )
            : "Sin acceso registrado";
    }
}
