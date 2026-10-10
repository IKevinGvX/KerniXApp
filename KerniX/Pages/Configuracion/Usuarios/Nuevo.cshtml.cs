using System.ComponentModel.DataAnnotations;

using KerniX.Models.Configuracion.Usuarios;
using KerniX.Security;
using KerniX.Services.Configuracion.Usuarios;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Configuracion.Usuarios;

public sealed class NuevoModel : PageModel
{
    private readonly IUsuarioAdminDbService _usuarioService;
    private readonly ILogger<NuevoModel> _logger;

    public NuevoModel(
        IUsuarioAdminDbService usuarioService,
        ILogger<NuevoModel> logger)
    {
        _usuarioService =
            usuarioService;

        _logger =
            logger;
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    public int IdUsuarioSesion { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }


    // =========================================================
    // FORMULARIO
    //
    // Propiedades directas para evitar problemas de binding
    // con objetos anidados.
    // =========================================================

    [BindProperty]
    public int IdEmpleado { get; set; }

    [BindProperty]
    public int IdRol { get; set; }

    [BindProperty]
    public string NombreUsuario { get; set; } =
        string.Empty;

    [BindProperty]
    public string Correo { get; set; } =
        string.Empty;

    [BindProperty]
    public string PasswordTemporal { get; set; } =
        string.Empty;

    [BindProperty]
    public string ConfirmarPassword { get; set; } =
        string.Empty;

    [BindProperty]
    public bool RequiereCambioClave { get; set; } =
        true;

    [BindProperty]
    public bool Estado { get; set; } =
        true;


    // =========================================================
    // CATÁLOGOS
    // =========================================================

    public IReadOnlyList<EmpleadoUsuarioDisponibleItem>
        Empleados
    { get; private set; } =
        Array.Empty<EmpleadoUsuarioDisponibleItem>();

    public IReadOnlyList<RolAdminItem>
        Roles
    { get; private set; } =
        Array.Empty<RolAdminItem>();


    // =========================================================
    // MENSAJES
    // =========================================================

    public string? MensajeError { get; private set; }


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
            await CargarCatalogosAsync(
                cancellationToken
            );

            RequiereCambioClave =
                true;

            Estado =
                true;


            _logger.LogInformation(
                "[USUARIO NUEVO] GET. Administrador={Usuario} Empresa={Empresa} Sucursal={Sucursal} Empleados={Empleados} Roles={Roles}",
                IdUsuarioSesion,
                IdEmpresa,
                IdSucursal,
                Empleados.Count,
                Roles.Count
            );


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIO NUEVO] Error cargando formulario."
            );


            MensajeError =
                "No fue posible cargar empleados y categorías.";


            return Page();
        }
    }


    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[USUARIO NUEVO] POST recibido."
        );


        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        // =====================================================
        // NORMALIZAR
        // =====================================================

        NombreUsuario =
            (NombreUsuario ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

        Correo =
            (Correo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

        PasswordTemporal ??=
            string.Empty;

        ConfirmarPassword ??=
            string.Empty;


        // =====================================================
        // VALIDAR
        // =====================================================

        ValidarFormulario();


        if (!ModelState.IsValid)
        {
            await CargarCatalogosAsync(
                cancellationToken
            );


            MensajeError =
                "Existen datos inválidos. Revisa el formulario.";


            foreach (
                var item in ModelState
                    .Where(
                        x =>
                            x.Value?.Errors.Count > 0
                    )
            )
            {
                foreach (
                    var error in
                    item.Value!.Errors
                )
                {
                    _logger.LogWarning(
                        "[USUARIO NUEVO] Campo={Campo} Error={Error}",
                        item.Key,
                        error.ErrorMessage
                    );
                }
            }


            return Page();
        }


        // =====================================================
        // REQUEST
        // =====================================================

        UsuarioCrearAdminRequest request =
            new()
            {
                IdEmpleado =
                    IdEmpleado,

                IdRol =
                    IdRol,

                NombreUsuario =
                    NombreUsuario,

                Correo =
                    Correo,

                PasswordTemporal =
                    PasswordTemporal,

                RequiereCambioClave =
                    RequiereCambioClave,

                Estado =
                    Estado,

                IdUsuarioRegistro =
                    IdUsuarioSesion
            };


        try
        {
            int idUsuario =
                await _usuarioService
                    .CrearUsuarioAsync(
                        request,
                        cancellationToken
                    );


            _logger.LogInformation(
                "[USUARIO NUEVO] Usuario creado correctamente. IdUsuario={Usuario} Empleado={Empleado} Rol={Rol}",
                idUsuario,
                IdEmpleado,
                IdRol
            );


            TempData["Mensaje"] =
                "Usuario registrado correctamente.";


            return RedirectToPage(
                "./Index"
            );
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIO NUEVO] SQL {Numero}: {Mensaje}",
                ex.Number,
                ex.Message
            );


            await CargarCatalogosAsync(
                cancellationToken
            );


            MensajeError =
                ex.Number switch
                {
                    62001 =>
                        "El empleado no existe o está inactivo.",

                    62002 =>
                        "El empleado ya tiene un usuario asignado.",

                    62003 =>
                        "El nombre de usuario ya está registrado.",

                    62004 =>
                        "El correo ya está registrado.",

                    62402 =>
                        "No fue posible identificar el usuario recién creado.",

                    62403 =>
                        "La categoría seleccionada no existe o está inactiva.",

                    _ =>
                        $"SQL {ex.Number}: {ex.Message}"
                };


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIO NUEVO] Error creando usuario."
            );


            await CargarCatalogosAsync(
                cancellationToken
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private void ValidarFormulario()
    {
        if (IdEmpleado <= 0)
        {
            ModelState.AddModelError(
                nameof(IdEmpleado),
                "Selecciona un empleado."
            );
        }


        if (IdRol <= 0)
        {
            ModelState.AddModelError(
                nameof(IdRol),
                "Selecciona una categoría."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                NombreUsuario
            )
        )
        {
            ModelState.AddModelError(
                nameof(NombreUsuario),
                "El nombre de usuario es obligatorio."
            );
        }
        else if (
            NombreUsuario.Length > 100
        )
        {
            ModelState.AddModelError(
                nameof(NombreUsuario),
                "El nombre de usuario no puede superar 100 caracteres."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                Correo
            )
        )
        {
            ModelState.AddModelError(
                nameof(Correo),
                "El correo es obligatorio."
            );
        }
        else if (
            Correo.Length > 200
        )
        {
            ModelState.AddModelError(
                nameof(Correo),
                "El correo no puede superar 200 caracteres."
            );
        }
        else if (
            !new EmailAddressAttribute()
                .IsValid(
                    Correo
                )
        )
        {
            ModelState.AddModelError(
                nameof(Correo),
                "Ingresa un correo válido."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                PasswordTemporal
            )
        )
        {
            ModelState.AddModelError(
                nameof(PasswordTemporal),
                "La contraseña temporal es obligatoria."
            );
        }
        else if (
            PasswordTemporal.Length < 8
        )
        {
            ModelState.AddModelError(
                nameof(PasswordTemporal),
                "La contraseña temporal debe tener al menos 8 caracteres."
            );
        }
        else if (
            PasswordTemporal.Length > 100
        )
        {
            ModelState.AddModelError(
                nameof(PasswordTemporal),
                "La contraseña temporal es demasiado larga."
            );
        }


        if (
            !string.Equals(
                PasswordTemporal,
                ConfirmarPassword,
                StringComparison.Ordinal
            )
        )
        {
            ModelState.AddModelError(
                nameof(ConfirmarPassword),
                "Las contraseñas no coinciden."
            );
        }
    }


    // =========================================================
    // CATÁLOGOS
    // =========================================================

    private async Task CargarCatalogosAsync(
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyList<EmpleadoUsuarioDisponibleItem>>
            empleadosTask =
                _usuarioService
                    .ListarEmpleadosDisponiblesAsync(
                        IdEmpresa,
                        IdSucursal,
                        cancellationToken
                    );


        Task<IReadOnlyList<RolAdminItem>>
            rolesTask =
                _usuarioService
                    .ListarRolesAsync(
                        IdEmpresa,
                        cancellationToken
                    );


        await Task.WhenAll(
            empleadosTask,
            rolesTask
        );


        Empleados =
            await empleadosTask;


        IReadOnlyList<RolAdminItem> roles =
            await rolesTask;


        Roles =
            roles
                .Where(
                    x =>
                        x.Estado
                        &&
                        (
                            x.EsAdministrador
                            ||
                            x.Nombre.Contains(
                                "venta",
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            x.Nombre.Contains(
                                "inventario",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                )
                .OrderByDescending(
                    x =>
                        x.EsAdministrador
                )
                .ThenBy(
                    x =>
                        x.Nombre
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
}
