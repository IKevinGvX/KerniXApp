using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class SeguridadModel : PageModel
{
    private readonly IParametroSistemaDbService _parametroService;
    private readonly ILogger<SeguridadModel> _logger;

    public SeguridadModel(
        IParametroSistemaDbService parametroService,
        ILogger<SeguridadModel> logger)
    {
        _parametroService = parametroService;
        _logger = logger;
    }

    // =========================================================
    // CONTEXTO
    // =========================================================

    [BindProperty]
    public int IdEmpresa { get; set; }

    // =========================================================
    // SEGURIDAD
    // =========================================================

    [BindProperty]
    public int? IntentosLoginMaximos { get; set; }

    [BindProperty]
    public int? MinutosBloqueo { get; set; }

    [BindProperty]
    public int? MinutosExpiracionSesion { get; set; }

    [BindProperty]
    public int? DiasVigenciaPassword { get; set; }

    [BindProperty]
    public bool RequerirCambioPassword { get; set; }

    [BindProperty]
    public int? LongitudMinimaPassword { get; set; }

    // =========================================================
    // MENSAJES
    // =========================================================

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return RedirectToPage(
                "/Account/Login");
        }

        IdEmpresa =
            ObtenerIdEmpresa();

        try
        {
            await CargarConfiguracionAsync();

            _logger.LogInformation(
                "Configuración Seguridad cargada. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración de seguridad.");

            MensajeError =
                $"No se pudo cargar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return RedirectToPage(
                "/Account/Login");
        }

        IdEmpresa =
            ObtenerIdEmpresa();

        ModelState.Clear();

        ValidarFormulario();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Configuración Seguridad inválida. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }

        try
        {
            await GuardarParametroAsync(
                "INTENTOS_LOGIN_MAXIMOS",
                IntentosLoginMaximos!.Value.ToString());

            await GuardarParametroAsync(
                "MINUTOS_BLOQUEO",
                MinutosBloqueo!.Value.ToString());

            await GuardarParametroAsync(
                "MINUTOS_EXPIRACION_SESION",
                MinutosExpiracionSesion!.Value.ToString());

            await GuardarParametroAsync(
                "DIAS_VIGENCIA_PASSWORD",
                DiasVigenciaPassword!.Value.ToString());

            await GuardarParametroAsync(
                "REQUERIR_CAMBIO_PASSWORD",
                BoolAValor(
                    RequerirCambioPassword));

            await GuardarParametroAsync(
                "LONGITUD_MINIMA_PASSWORD",
                LongitudMinimaPassword!.Value.ToString());

            MensajeExito =
                "Configuración de seguridad guardada correctamente.";

            _logger.LogInformation(
                "Configuración Seguridad guardada. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return RedirectToPage(
                "/Configuracion/Seguridad");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando configuración de seguridad.");

            MensajeError =
                $"No se pudo guardar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // CARGAR
    // =========================================================

    private async Task CargarConfiguracionAsync()
    {
        var parametros =
            await _parametroService.ListarAsync(
                IdEmpresa);

        string Obtener(
            string codigo,
            string valorDefault)
        {
            var parametro =
                parametros.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Codigo,
                            codigo,
                            StringComparison.OrdinalIgnoreCase));

            if (parametro is null ||
                string.IsNullOrWhiteSpace(
                    parametro.Valor))
            {
                return valorDefault;
            }

            return parametro.Valor.Trim();
        }

        IntentosLoginMaximos =
            ObtenerEntero(
                Obtener(
                    "INTENTOS_LOGIN_MAXIMOS",
                    "5"),
                5);

        MinutosBloqueo =
            ObtenerEntero(
                Obtener(
                    "MINUTOS_BLOQUEO",
                    "15"),
                15);

        MinutosExpiracionSesion =
            ObtenerEntero(
                Obtener(
                    "MINUTOS_EXPIRACION_SESION",
                    "30"),
                30);

        DiasVigenciaPassword =
            ObtenerEntero(
                Obtener(
                    "DIAS_VIGENCIA_PASSWORD",
                    "90"),
                90);

        RequerirCambioPassword =
            ObtenerBool(
                Obtener(
                    "REQUERIR_CAMBIO_PASSWORD",
                    "0"));

        LongitudMinimaPassword =
            ObtenerEntero(
                Obtener(
                    "LONGITUD_MINIMA_PASSWORD",
                    "8"),
                8);
    }

    // =========================================================
    // GUARDAR
    // =========================================================

    private async Task GuardarParametroAsync(
        string codigo,
        string valor)
    {
        var request =
            new ParametroSistemaActualizarValorRequest
            {
                IdEmpresa = IdEmpresa,
                Codigo = codigo,
                Valor = valor
            };

        await _parametroService
            .ActualizarValorAsync(
                request);
    }

    // =========================================================
    // VALIDACIONES
    // =========================================================

    private void ValidarFormulario()
    {
        if (IdEmpresa <= 0)
        {
            ModelState.AddModelError(
                nameof(IdEmpresa),
                "No se pudo determinar la empresa.");
        }

        if (!IntentosLoginMaximos.HasValue)
        {
            ModelState.AddModelError(
                nameof(IntentosLoginMaximos),
                "Ingrese el número máximo de intentos.");
        }
        else if (
            IntentosLoginMaximos.Value < 1 ||
            IntentosLoginMaximos.Value > 20)
        {
            ModelState.AddModelError(
                nameof(IntentosLoginMaximos),
                "Los intentos deben estar entre 1 y 20.");
        }

        if (!MinutosBloqueo.HasValue)
        {
            ModelState.AddModelError(
                nameof(MinutosBloqueo),
                "Ingrese los minutos de bloqueo.");
        }
        else if (
            MinutosBloqueo.Value < 1 ||
            MinutosBloqueo.Value > 1440)
        {
            ModelState.AddModelError(
                nameof(MinutosBloqueo),
                "El bloqueo debe estar entre 1 y 1440 minutos.");
        }

        if (!MinutosExpiracionSesion.HasValue)
        {
            ModelState.AddModelError(
                nameof(MinutosExpiracionSesion),
                "Ingrese la duración de la sesión.");
        }
        else if (
            MinutosExpiracionSesion.Value < 5 ||
            MinutosExpiracionSesion.Value > 1440)
        {
            ModelState.AddModelError(
                nameof(MinutosExpiracionSesion),
                "La sesión debe durar entre 5 y 1440 minutos.");
        }

        if (!DiasVigenciaPassword.HasValue)
        {
            ModelState.AddModelError(
                nameof(DiasVigenciaPassword),
                "Ingrese los días de vigencia.");
        }
        else if (
            DiasVigenciaPassword.Value < 0 ||
            DiasVigenciaPassword.Value > 3650)
        {
            ModelState.AddModelError(
                nameof(DiasVigenciaPassword),
                "La vigencia de contraseña no es válida.");
        }

        if (!LongitudMinimaPassword.HasValue)
        {
            ModelState.AddModelError(
                nameof(LongitudMinimaPassword),
                "Ingrese la longitud mínima.");
        }
        else if (
            LongitudMinimaPassword.Value < 6 ||
            LongitudMinimaPassword.Value > 128)
        {
            ModelState.AddModelError(
                nameof(LongitudMinimaPassword),
                "La longitud debe estar entre 6 y 128 caracteres.");
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private int ObtenerIdEmpresa()
    {
        int idEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa") ?? 0;

        if (idEmpresa <= 0)
        {
            idEmpresa = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                idEmpresa);
        }

        return idEmpresa;
    }

    private static bool ObtenerBool(
        string valor)
    {
        return
            valor == "1" ||
            valor.Equals(
                "true",
                StringComparison.OrdinalIgnoreCase);
    }

    private static int ObtenerEntero(
        string valor,
        int valorDefault)
    {
        return int.TryParse(
            valor,
            out int resultado)
                ? resultado
                : valorDefault;
    }

    private static string BoolAValor(
        bool valor)
    {
        return valor
            ? "1"
            : "0";
    }
}