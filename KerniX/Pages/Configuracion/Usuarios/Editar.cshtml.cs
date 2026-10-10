using System.ComponentModel.DataAnnotations;

using KerniX.Models.Configuracion.Usuarios;
using KerniX.Security;
using KerniX.Services.Configuracion.Usuarios;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Configuracion.Usuarios;

public sealed class EditarModel : PageModel
{
    private readonly IUsuarioAdminDbService _usuarioService;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(
        IUsuarioAdminDbService usuarioService,
        ILogger<EditarModel> logger)
    {
        _usuarioService =
            usuarioService;

        _logger =
            logger;
    }


    public int IdUsuarioSesion { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }


    // =========================================================
    // DATOS DEL USUARIO
    // =========================================================

    public UsuarioAdminEditarDetalle? Usuario { get; private set; }

    // =========================================================
    // DATOS DE PRESENTACIÓN
    //
    // La vista usa estas propiedades simples y no depende de
    // Model.Usuario para renderizar el formulario.
    // =========================================================

    public string EmpleadoTexto { get; private set; } =
        string.Empty;

    public string EmpresaTexto { get; private set; } =
        string.Empty;

    public string SucursalTexto { get; private set; } =
        string.Empty;

    public string CargoTexto { get; private set; } =
        string.Empty;

    public string RolTexto { get; private set; } =
        string.Empty;

    public int IntentosFallidosTexto { get; private set; }

    public DateTime? BloqueadoHastaTexto { get; private set; }

    public DateTime? UltimoAccesoTexto { get; private set; }


    // =========================================================
    // FORMULARIO EDITAR
    // =========================================================

    [BindProperty]
    public int IdUsuario { get; set; }

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
    public bool Estado { get; set; }

    [BindProperty]
    public bool RequiereCambioClave { get; set; }


    // =========================================================
    // RESTABLECER CLAVE
    // =========================================================

    [BindProperty]
    public string PasswordTemporal { get; set; } =
        string.Empty;

    [BindProperty]
    public string ConfirmarPassword { get; set; } =
        string.Empty;

    [BindProperty]
    public bool ForzarCambioClave { get; set; } =
        true;


    // =========================================================
    // CATÁLOGO
    // =========================================================

    public IReadOnlyList<RolAdminItem> Roles { get; private set; } =
        Array.Empty<RolAdminItem>();


    // =========================================================
    // MENSAJES
    // =========================================================

    [TempData]
    public string? Mensaje { get; set; }

    public string? MensajeError { get; private set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        int idUsuario,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[USUARIO EDITAR] GET recibido. IdUsuario={IdUsuario}",
            idUsuario
        );


        if (!CargarSesion())
        {
            _logger.LogWarning(
                "[USUARIO EDITAR] Sesión inválida."
            );

            return RedirectToPage(
                "/Account/Login"
            );
        }


        _logger.LogInformation(
            "[USUARIO EDITAR] Contexto. Admin={Admin} Empresa={Empresa} Sucursal={Sucursal}",
            IdUsuarioSesion,
            IdEmpresa,
            IdSucursal
        );


        if (idUsuario <= 0)
        {
            MensajeError =
                "No se recibió un IdUsuario válido.";

            return Page();
        }


        // Conservar el ID recibido aunque la consulta falle.
        // Así la vista puede reintentar exactamente el mismo usuario.
        IdUsuario =
            idUsuario;


        try
        {
            bool cargado =
                await CargarUsuarioAsync(
                    idUsuario,
                    cargarFormulario: true,
                    cancellationToken
                );


            if (!cargado)
            {
                _logger.LogWarning(
                    "[USUARIO EDITAR] Usuario no encontrado. Usuario={Usuario} Empresa={Empresa}",
                    idUsuario,
                    IdEmpresa
                );

                TempData["Error"] =
                    $"No se encontró el usuario {idUsuario} para la empresa {IdEmpresa}.";

                return RedirectToPage(
                    "./Index"
                );
            }


            _logger.LogInformation(
                "[USUARIO EDITAR] Usuario cargado. Usuario={Usuario} Empleado={Empleado} Rol={Rol} Estado={Estado}",
                Usuario!.IdUsuario,
                Usuario.IdEmpleado,
                Usuario.IdRol,
                Usuario.Estado
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[USUARIO EDITAR] Error SQL cargando Usuario={Usuario}. SQL={Numero}",
                idUsuario,
                ex.Number
            );

            MensajeError =
                $"SQL {ex.Number}: {ex.Message}";

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIO EDITAR] Error cargando Usuario={Usuario}",
                idUsuario
            );

            MensajeError =
                ex.Message;

            return Page();
        }
    }


    // =========================================================
    // POST - GUARDAR CAMBIOS
    // =========================================================

    public async Task<IActionResult> OnPostGuardarAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        NombreUsuario =
            (NombreUsuario ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

        Correo =
            (Correo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();


        // No validar formulario de password en este handler.
        ModelState.Remove(
            nameof(PasswordTemporal)
        );

        ModelState.Remove(
            nameof(ConfirmarPassword)
        );


        ValidarEditar();


        if (!ModelState.IsValid)
        {
            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: false,
                cancellationToken
            );

            MensajeError =
                "Revisa los datos ingresados.";

            return Page();
        }


        // Evitar que el administrador se desactive a sí mismo.
        if (
            IdUsuario ==
            IdUsuarioSesion
        )
        {
            Estado =
                true;


            // También conservamos su categoría actual para evitar
            // dejar al usuario administrador sin acceso al panel.
            if (
                Usuario?.IdRol.HasValue ==
                true
            )
            {
                IdRol =
                    Usuario.IdRol.Value;
            }
        }


        UsuarioAdminActualizarRequest request =
            new()
            {
                IdUsuario =
                    IdUsuario,

                IdEmpleado =
                    IdEmpleado,

                IdRol =
                    IdRol,

                NombreUsuario =
                    NombreUsuario,

                Correo =
                    Correo,

                Estado =
                    Estado,

                RequiereCambioClave =
                    RequiereCambioClave,

                IdUsuarioModifica =
                    IdUsuarioSesion
            };


        try
        {
            await _usuarioService
                .ActualizarUsuarioAsync(
                    request,
                    cancellationToken
                );


            TempData["Mensaje"] =
                "Usuario actualizado correctamente.";


            return RedirectToPage(
                "./Editar",
                new
                {
                    idUsuario =
                        IdUsuario
                }
            );
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(
                ex,
                "[USUARIO EDITAR] SQL {Numero}. Usuario={Usuario}",
                ex.Number,
                IdUsuario
            );


            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: false,
                cancellationToken
            );


            MensajeError =
                ex.Number switch
                {
                    62101 =>
                        "El usuario no existe.",

                    62102 =>
                        "El empleado no existe o está inactivo.",

                    62103 =>
                        "El nombre de usuario ya está registrado.",

                    62104 =>
                        "El correo ya está registrado.",

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
                "[USUARIO EDITAR] Error actualizando Usuario={Usuario}",
                IdUsuario
            );


            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: false,
                cancellationToken
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    // =========================================================
    // POST - RESTABLECER CLAVE
    // =========================================================

    public async Task<IActionResult> OnPostRestablecerClaveAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        // Ignorar las validaciones del formulario principal.
        ModelState.Clear();


        PasswordTemporal ??=
            string.Empty;

        ConfirmarPassword ??=
            string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                PasswordTemporal
            )
        )
        {
            MensajeError =
                "Ingresa una contraseña temporal.";

            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: true,
                cancellationToken
            );

            return Page();
        }


        if (
            PasswordTemporal.Length < 8
        )
        {
            MensajeError =
                "La contraseña debe tener al menos 8 caracteres.";

            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: true,
                cancellationToken
            );

            return Page();
        }


        if (
            !string.Equals(
                PasswordTemporal,
                ConfirmarPassword,
                StringComparison.Ordinal
            )
        )
        {
            MensajeError =
                "Las contraseñas no coinciden.";

            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: true,
                cancellationToken
            );

            return Page();
        }


        try
        {
            await _usuarioService
                .RestablecerClaveAsync(
                    IdUsuario,
                    PasswordTemporal,
                    ForzarCambioClave,
                    cancellationToken
                );


            TempData["Mensaje"] =
                "Contraseña temporal restablecida correctamente.";


            return RedirectToPage(
                "./Editar",
                new
                {
                    idUsuario =
                        IdUsuario
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[USUARIO EDITAR] Error restableciendo clave. Usuario={Usuario}",
                IdUsuario
            );


            await CargarUsuarioAsync(
                IdUsuario,
                cargarFormulario: true,
                cancellationToken
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    // =========================================================
    // VALIDAR
    // =========================================================

    private void ValidarEditar()
    {
        if (IdUsuario <= 0)
        {
            ModelState.AddModelError(
                nameof(IdUsuario),
                "Usuario inválido."
            );
        }


        if (IdEmpleado <= 0)
        {
            ModelState.AddModelError(
                nameof(IdEmpleado),
                "Empleado inválido."
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
                "El usuario es obligatorio."
            );
        }
        else if (
            NombreUsuario.Length > 100
        )
        {
            ModelState.AddModelError(
                nameof(NombreUsuario),
                "El usuario no puede superar 100 caracteres."
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
    }


    // =========================================================
    // CARGAR USUARIO
    // =========================================================

    private async Task<bool> CargarUsuarioAsync(
        int idUsuario,
        bool cargarFormulario,
        CancellationToken cancellationToken)
    {
        Usuario =
            await _usuarioService
                .ObtenerParaEditarAsync(
                    idUsuario,
                    IdEmpresa,
                    cancellationToken
                );


        if (Usuario is null)
        {
            return false;
        }


        // =====================================================
        // COPIA ESTABLE PARA LA VISTA
        // =====================================================

        EmpleadoTexto =
            Usuario.Empleado;

        EmpresaTexto =
            Usuario.Empresa;

        SucursalTexto =
            Usuario.Sucursal;

        CargoTexto =
            Usuario.Cargo;

        RolTexto =
            Usuario.Rol;

        IntentosFallidosTexto =
            Usuario.IntentosFallidos;

        BloqueadoHastaTexto =
            Usuario.BloqueadoHasta;

        UltimoAccesoTexto =
            Usuario.UltimoAcceso;


        IReadOnlyList<RolAdminItem> roles =
            await _usuarioService
                .ListarRolesAsync(
                    IdEmpresa,
                    cancellationToken
                );


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


        if (cargarFormulario)
        {
            IdUsuario =
                Usuario.IdUsuario;

            IdEmpleado =
                Usuario.IdEmpleado;

            IdRol =
                Usuario.IdRol ?? 0;

            NombreUsuario =
                Usuario.NombreUsuario;

            Correo =
                Usuario.Correo;

            Estado =
                Usuario.Estado;

            RequiereCambioClave =
                Usuario.RequiereCambioClave;

            ForzarCambioClave =
                true;
        }


        return true;
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
