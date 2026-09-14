using System.ComponentModel.DataAnnotations;
using KerniX.DTOs.Auth;
using KerniX.Security;
using KerniX.Services.Auth;
using KerniX.Services.Empleados;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
namespace KerniX.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IAuthApiService _authService;
    private readonly IEmpleadoDbService _empleadoDbService;
    private readonly TokenStorageService _tokenStorage;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        IAuthApiService authService,
        IEmpleadoDbService empleadoDbService,
        TokenStorageService tokenStorage,
        ILogger<LoginModel> logger)
    {
        _authService = authService;
        _empleadoDbService = empleadoDbService;
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public IActionResult OnGet()
    {
        try
        {
            if (_tokenStorage.EstaAutenticado())
            {
                return RedirectToPage("/Inicio/Carga");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No fue posible comprobar la sesión existente.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /Account/Login recibido.");

        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x =>
                    x.Value!.Errors.Select(
                        error => error.ErrorMessage))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            ErrorMessage =
                errores.Count > 0
                    ? string.Join(" ", errores)
                    : "Completa correctamente los campos.";

            _logger.LogWarning(
                "Login rechazado por ModelState: {Errores}",
                ErrorMessage);

            return Page();
        }

        if (string.IsNullOrWhiteSpace(Input.Usuario))
        {
            ErrorMessage =
                "Ingresa tu usuario o correo electrónico.";

            return Page();
        }

        if (string.IsNullOrWhiteSpace(Input.Password))
        {
            ErrorMessage =
                "Ingresa tu contraseña.";

            return Page();
        }

        try
        {
            var usuarioIngresado =
                Input.Usuario.Trim();

            var request =
                new LoginRequest
                {
                    Usuario = usuarioIngresado,
                    Password = Input.Password
                };

            _logger.LogInformation(
                "Intentando autenticar usuario {Usuario}.",
                usuarioIngresado);

            var resultado =
                await _authService.LoginAsync(
                    request,
                    cancellationToken);

            if (resultado is null)
            {
                ErrorMessage =
                    "KerniX API no devolvió una respuesta.";

                _logger.LogWarning(
                    "LoginAsync devolvió null.");

                return Page();
            }

            if (!resultado.Success)
            {
                ErrorMessage =
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Usuario o contraseña incorrectos."
                        : resultado.Message;

                _logger.LogWarning(
                    "Inicio de sesión rechazado para {Usuario}.",
                    usuarioIngresado);

                return Page();
            }

            if (resultado.Data is null)
            {
                ErrorMessage =
                    "La autenticación fue correcta, pero no se recibieron datos de sesión.";

                _logger.LogError(
                    "Login correcto sin Data.");

                return Page();
            }

            var auth = resultado.Data;

            if (string.IsNullOrWhiteSpace(auth.AccessToken))
            {
                ErrorMessage =
                    "La autenticación fue correcta, pero no se recibió un token de acceso.";

                _logger.LogError(
                    "Login correcto sin AccessToken.");

                return Page();
            }

            _tokenStorage.GuardarToken(auth.AccessToken);

            if (!string.IsNullOrWhiteSpace(auth.RefreshToken))
            {
                _tokenStorage.GuardarRefreshToken(
                    auth.RefreshToken);
            }

            var usuarioAutenticado =
               usuarioIngresado;

            HttpContext.Session.SetString(
                SessionKeys.Usuario,
                usuarioAutenticado);
            _logger.LogInformation(
                "Consultando Usuario + Empleado en SQL Server para {Usuario}.",
                usuarioAutenticado);

            var identidad =
                await _empleadoDbService.ObtenerPorUsuarioAsync(
                    usuarioAutenticado,
                    cancellationToken);

            if (identidad is null)
            {
                HttpContext.Session.SetString(
                    SessionKeys.Nombre,
                    usuarioAutenticado);

                HttpContext.Session.SetString(
                    SessionKeys.NombreCompleto,
                    usuarioAutenticado);

                HttpContext.Session.SetString(
                    SessionKeys.Cargo,
                    string.Empty);

                HttpContext.Session.SetString(
                    SessionKeys.Correo,
                    string.Empty);

                _logger.LogWarning(
                    "Usuario {Usuario} autenticado, pero no se encontró relación Usuario/Empleado en SQL Server.",
                    usuarioAutenticado);

                return RedirectToPage("/Inicio/Carga");
            }

            HttpContext.Session.SetInt32(
                SessionKeys.IdUsuario,
                identidad.IdUsuario);

            HttpContext.Session.SetString(
                SessionKeys.Usuario,
                string.IsNullOrWhiteSpace(identidad.Usuario)
                    ? usuarioAutenticado
                    : identidad.Usuario.Trim());

            if (identidad.IdEmpleado.HasValue &&
                identidad.IdEmpleado.Value > 0)
            {
                HttpContext.Session.SetInt32(
                    SessionKeys.IdEmpleado,
                    identidad.IdEmpleado.Value);
            }

            var nombre =
                identidad.Nombre?.Trim()
                ?? string.Empty;

            var apellidoPaterno =
                identidad.ApellidoPaterno?.Trim()
                ?? string.Empty;

            var apellidoMaterno =
                identidad.ApellidoMaterno?.Trim()
                ?? string.Empty;

            var nombreCompleto =
                string.Join(
                    " ",
                    new[]
                    {
                        nombre,
                        apellidoPaterno,
                        apellidoMaterno
                    }
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x)));

            HttpContext.Session.SetString(
                SessionKeys.Nombre,
                nombre);

            HttpContext.Session.SetString(
                SessionKeys.ApellidoPaterno,
                apellidoPaterno);

            HttpContext.Session.SetString(
                SessionKeys.NombreCompleto,
                string.IsNullOrWhiteSpace(nombreCompleto)
                    ? usuarioAutenticado
                    : nombreCompleto);

            HttpContext.Session.SetString(
                SessionKeys.Cargo,
                identidad.Cargo?.Trim()
                ?? string.Empty);

            HttpContext.Session.SetString(
                SessionKeys.Correo,
                identidad.Correo?.Trim()
                ?? string.Empty);

            _logger.LogInformation(
                "Login completo. Usuario: {Usuario} | IdUsuario: {IdUsuario} | IdEmpleado: {IdEmpleado} | Nombre: {NombreCompleto}.",
                identidad.Usuario,
                identidad.IdUsuario,
                identidad.IdEmpleado,
                nombreCompleto);

            return RedirectToPage("/Inicio/Carga");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error HTTP conectando con KerniX Auth API.");

            ErrorMessage =
                "No se pudo conectar con el servicio de autenticación.";

            return Page();
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Timeout durante el inicio de sesión.");

            ErrorMessage =
                "El servicio de autenticación tardó demasiado en responder.";

            return Page();
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "La solicitud de inicio de sesión fue cancelada.";

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado durante el inicio de sesión.");

            ErrorMessage =
                "Ocurrió un error interno al iniciar sesión.";

            return Page();
        }
    }

    public sealed class LoginInput
    {
        [Required(
            ErrorMessage =
                "Ingresa tu usuario.")]
        [StringLength(
            100,
            ErrorMessage =
                "El usuario es demasiado largo.")]
        public string Usuario { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage =
                "Ingresa tu contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
            = string.Empty;

        public bool Recordarme { get; set; }
    }

}