using System.ComponentModel.DataAnnotations;
using KerniX.Models.Usuarios;
using KerniX.Security;
using KerniX.Services.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Perfil;

public sealed class IndexModel : PageModel
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly IUsuarioService _usuarioService;
    private readonly TokenStorageService _tokenStorage;
    private readonly ILogger<IndexModel> _logger;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public IndexModel(
        IUsuarioService usuarioService,
        TokenStorageService tokenStorage,
        ILogger<IndexModel> logger)
    {
        _usuarioService = usuarioService;
        _tokenStorage = tokenStorage;
        _logger = logger;
    }


    // =========================================================
    // PERFIL
    // =========================================================

    public UsuarioPerfilResponse Perfil { get; set; }
        = new();


    // =========================================================
    // EDITAR
    // =========================================================

    [BindProperty]
    public EditarPerfilInput Editar { get; set; }
        = new();


    // =========================================================
    // PASSWORD
    // =========================================================

    [BindProperty]
    public CambiarPasswordInput Password { get; set; }
        = new();


    // =========================================================
    // MODALES
    // =========================================================

    public bool AbrirModalEditar { get; set; }

    public bool AbrirModalPassword { get; set; }


    // =========================================================
    // MENSAJES
    // =========================================================

    [TempData]
    public string? Mensaje { get; set; }

    [TempData]
    public string? Error { get; set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync()
    {
        int? idUsuario =
            ObtenerIdUsuario();

        if (idUsuario is null)
        {
            return RedirectToPage(
                "/Account/Login");
        }

        bool cargado =
            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: true);

        if (!cargado)
        {
            Error =
                "No se pudo cargar el perfil desde la API.";
        }
        else
        {
            Error = null;
        }

        return Page();
    }


    // =========================================================
    // POST - ACTUALIZAR PERFIL
    // =========================================================

    public async Task<IActionResult>
        OnPostActualizarPerfilAsync()
    {
        _logger.LogWarning(
            "========== ENTRÓ OnPostActualizarPerfilAsync ==========");

        int? idUsuario =
            ObtenerIdUsuario();

        if (idUsuario is null)
        {
            return RedirectToPage(
                "/Account/Login");
        }


        // =====================================================
        // VALIDAR SOLO EL FORMULARIO EDITAR
        // =====================================================

        ModelState.Clear();

        bool editarValido =
            TryValidateModel(
                Editar,
                nameof(Editar));


        if (!editarValido)
        {
            string[] errores =
                ModelState
                    .Where(x =>
                        x.Value is not null &&
                        x.Value.Errors.Count > 0)
                    .SelectMany(x =>
                        x.Value!.Errors.Select(
                            e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Dato inválido."
                                    : e.ErrorMessage))
                    .ToArray();


            _logger.LogWarning(
                "EDITAR PERFIL INVALIDO -> {Errores}",
                string.Join(
                    " | ",
                    errores));


            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: false);


            Error =
                errores.Length > 0
                    ? string.Join(
                        " ",
                        errores)
                    : "Revisa los datos ingresados.";


            AbrirModalEditar =
                true;


            return Page();
        }


        try
        {
            // =================================================
            // OBTENER PERFIL ACTUAL
            // =================================================

            UsuarioPerfilResponse? perfilActual =
                await _usuarioService
                    .ObtenerPerfilAsync(
                        idUsuario.Value);


            if (perfilActual is null)
            {
                Error =
                    "No se pudo obtener el usuario actual.";

                AbrirModalEditar =
                    true;

                return Page();
            }


            // =================================================
            // REQUEST API
            // =================================================

            ActualizarUsuarioRequest request =
                new()
                {
                    IdEmpleado =
                        perfilActual.IdEmpleado,

                    NombreUsuario =
                        Editar.NombreUsuario.Trim(),

                    Correo =
                        Editar.Correo.Trim(),

                    Estado =
                        perfilActual.Estado,

                    RequiereCambioClave =
                        false
                };


            _logger.LogWarning(
                "PUT PERFIL -> IdUsuario={IdUsuario}, IdEmpleado={IdEmpleado}, Usuario={Usuario}, Correo={Correo}",
                idUsuario.Value,
                request.IdEmpleado,
                request.NombreUsuario,
                request.Correo);


            // =================================================
            // API
            // =================================================

            ApiOperationResult resultado =
                await _usuarioService
                    .ActualizarPerfilAsync(
                        idUsuario.Value,
                        request);


            _logger.LogWarning(
                "RESULTADO PUT -> Success={Success} Message={Message}",
                resultado.Success,
                resultado.Message);


            if (!resultado.Success)
            {
                Perfil =
                    perfilActual;


                Error =
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "La API rechazó la actualización."
                        : resultado.Message;


                AbrirModalEditar =
                    true;


                return Page();
            }


            // =================================================
            // ACTUALIZAR SESSION
            // =================================================

            HttpContext.Session.SetString(
                SessionKeys.Usuario,
                request.NombreUsuario);


            HttpContext.Session.SetString(
                SessionKeys.Correo,
                request.Correo);


            // =================================================
            // OK
            // =================================================

            Mensaje =
                string.IsNullOrWhiteSpace(
                    resultado.Message)
                    ? "Perfil actualizado correctamente."
                    : resultado.Message;


            return RedirectToPage(
                "/Perfil/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando usuario {IdUsuario}.",
                idUsuario.Value);


            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: false);


            Error =
                "Ocurrió un error al actualizar el perfil.";


            AbrirModalEditar =
                true;


            return Page();
        }
    }


    // =========================================================
    // POST - CAMBIAR CONTRASEÑA
    // =========================================================

    public async Task<IActionResult>
        OnPostCambiarPasswordAsync()
    {
        _logger.LogWarning(
            "========== ENTRÓ OnPostCambiarPasswordAsync ==========");


        int? idUsuario =
            ObtenerIdUsuario();


        if (idUsuario is null)
        {
            return RedirectToPage(
                "/Account/Login");
        }


        // =====================================================
        // VALIDAR SOLO PASSWORD
        // =====================================================

        ModelState.Clear();


        bool passwordValido =
            TryValidateModel(
                Password,
                nameof(Password));


        if (!passwordValido)
        {
            string[] errores =
                ModelState
                    .Where(x =>
                        x.Value is not null &&
                        x.Value.Errors.Count > 0)
                    .SelectMany(x =>
                        x.Value!.Errors.Select(
                            e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Dato inválido."
                                    : e.ErrorMessage))
                    .ToArray();


            _logger.LogWarning(
                "PASSWORD INVALIDO -> {Errores}",
                string.Join(
                    " | ",
                    errores));


            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: false);


            Error =
                errores.Length > 0
                    ? string.Join(
                        " ",
                        errores)
                    : "Revisa los datos de la contraseña.";


            AbrirModalPassword =
                true;


            return Page();
        }


        // =====================================================
        // VALIDAR CONFIRMACIÓN
        // =====================================================

        if (!string.Equals(
            Password.PasswordNueva,
            Password.ConfirmarPassword,
            StringComparison.Ordinal))
        {
            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: false);


            Error =
                "Las contraseñas no coinciden.";


            AbrirModalPassword =
                true;


            return Page();
        }


        try
        {
            // =================================================
            // REQUEST
            // =================================================

            CambiarPasswordRequest request =
                new()
                {
                    PasswordActual =
                        Password.PasswordActual,

                    PasswordNueva =
                        Password.PasswordNueva,

                    ConfirmarPassword =
                        Password.ConfirmarPassword
                };


            _logger.LogWarning(
                "PATCH PASSWORD -> IdUsuario={IdUsuario}",
                idUsuario.Value);


            // =================================================
            // API
            // =================================================

            ApiOperationResult resultado =
                await _usuarioService
                    .CambiarPasswordAsync(
                        idUsuario.Value,
                        request);


            _logger.LogWarning(
                "RESULTADO PATCH -> Success={Success} Message={Message}",
                resultado.Success,
                resultado.Message);


            if (!resultado.Success)
            {
                await CargarPerfilAsync(
                    idUsuario.Value,
                    cargarEditar: false);


                Error =
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "No se pudo cambiar la contraseña."
                        : resultado.Message;


                AbrirModalPassword =
                    true;


                return Page();
            }


            // =================================================
            // OK
            // =================================================

            Mensaje =
                string.IsNullOrWhiteSpace(
                    resultado.Message)
                    ? "Contraseña actualizada correctamente."
                    : resultado.Message;


            return RedirectToPage(
                "/Perfil/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando contraseña usuario {IdUsuario}.",
                idUsuario.Value);


            await CargarPerfilAsync(
                idUsuario.Value,
                cargarEditar: false);


            Error =
                "Ocurrió un error al cambiar la contraseña.";


            AbrirModalPassword =
                true;


            return Page();
        }
    }


    // =========================================================
    // CARGAR PERFIL
    // =========================================================

    private async Task<bool>
        CargarPerfilAsync(
            int idUsuario,
            bool cargarEditar)
    {
        try
        {
            UsuarioPerfilResponse? perfil =
                await _usuarioService
                    .ObtenerPerfilAsync(
                        idUsuario);


            if (perfil is null)
            {
                Perfil =
                    new UsuarioPerfilResponse();

                return false;
            }


            Perfil =
                perfil;


            // =================================================
            // PRECARGAR FORMULARIO EDITAR
            // =================================================

            if (cargarEditar ||
                string.IsNullOrWhiteSpace(
                    Editar.NombreUsuario))
            {
                Editar =
                    new EditarPerfilInput
                    {
                        NombreUsuario =
                            perfil.NombreUsuario,

                        Correo =
                            perfil.Correo
                    };
            }


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando perfil usuario {IdUsuario}.",
                idUsuario);


            Perfil =
                new UsuarioPerfilResponse();


            return false;
        }
    }


    // =========================================================
    // ID USUARIO
    // =========================================================

    private int? ObtenerIdUsuario()
    {
        if (!_tokenStorage
            .EstaAutenticado())
        {
            return null;
        }


        int idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario)
            ?? 0;


        if (idUsuario <= 0)
        {
            return null;
        }


        return idUsuario;
    }


    // =========================================================
    // INPUT EDITAR
    // =========================================================

    public sealed class EditarPerfilInput
    {
        [Required(
            ErrorMessage =
                "El nombre de usuario es obligatorio.")]
        [StringLength(
            100,
            MinimumLength = 3,
            ErrorMessage =
                "El usuario debe contener entre 3 y 100 caracteres.")]
        public string NombreUsuario { get; set; }
            = string.Empty;


        [Required(
            ErrorMessage =
                "El correo electrónico es obligatorio.")]
        [EmailAddress(
            ErrorMessage =
                "El correo electrónico no es válido.")]
        [StringLength(
            200,
            ErrorMessage =
                "El correo no puede superar los 200 caracteres.")]
        public string Correo { get; set; }
            = string.Empty;
    }


    // =========================================================
    // INPUT PASSWORD
    // =========================================================

    public sealed class CambiarPasswordInput
    {
        [Required(
            ErrorMessage =
                "Ingresa tu contraseña actual.")]
        public string PasswordActual { get; set; }
            = string.Empty;


        [Required(
            ErrorMessage =
                "Ingresa una nueva contraseña.")]
        [MinLength(
            8,
            ErrorMessage =
                "La contraseña debe tener mínimo 8 caracteres.")]
        public string PasswordNueva { get; set; }
            = string.Empty;


        [Required(
            ErrorMessage =
                "Confirma tu nueva contraseña.")]
        [Compare(
            nameof(PasswordNueva),
            ErrorMessage =
                "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; }
            = string.Empty;
    }
}