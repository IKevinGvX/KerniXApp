using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class IAModel : PageModel
{
    private readonly IParametroSistemaDbService _parametroService;
    private readonly ILogger<IAModel> _logger;

    public IAModel(
        IParametroSistemaDbService parametroService,
        ILogger<IAModel> logger)
    {
        _parametroService = parametroService;
        _logger = logger;
    }

    // =========================================================
    // PROPIEDADES
    // =========================================================

    [BindProperty]
    public int IdEmpresa { get; set; }

    [BindProperty]
    public bool IaActiva { get; set; }

    [BindProperty]
    public bool IaPronosticoVentas { get; set; }

    [BindProperty]
    public bool IaRecomendacionReposicion { get; set; }

    [BindProperty]
    public bool IaAlertasAnomalias { get; set; }

    [BindProperty]
    public int? DiasHistoricoPronostico { get; set; }

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
                "Configuración IA cargada | " +
                "Usuario={IdUsuario} | " +
                "Empresa={IdEmpresa} | " +
                "IA={IaActiva} | " +
                "Pronostico={Pronostico} | " +
                "Reposicion={Reposicion} | " +
                "Anomalias={Anomalias} | " +
                "Dias={Dias}",
                idUsuario.Value,
                IdEmpresa,
                IaActiva,
                IaPronosticoVentas,
                IaRecomendacionReposicion,
                IaAlertasAnomalias,
                DiasHistoricoPronostico);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración IA.");

            MensajeError =
                $"No se pudo cargar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // POST GUARDAR
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

        _logger.LogWarning(
            "IA POST => " +
            "Activa={Activa} | " +
            "Pronostico={Pronostico} | " +
            "Reposicion={Reposicion} | " +
            "Anomalias={Anomalias} | " +
            "Dias={Dias}",
            IaActiva,
            IaPronosticoVentas,
            IaRecomendacionReposicion,
            IaAlertasAnomalias,
            DiasHistoricoPronostico);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await GuardarParametroAsync(
                "IA_ACTIVA",
                BoolAValor(
                    IaActiva));

            await GuardarParametroAsync(
                "IA_PRONOSTICO_VENTAS",
                BoolAValor(
                    IaPronosticoVentas));

            await GuardarParametroAsync(
                "IA_RECOMENDACION_REPOSICION",
                BoolAValor(
                    IaRecomendacionReposicion));

            await GuardarParametroAsync(
                "IA_ALERTAS_ANOMALIAS",
                BoolAValor(
                    IaAlertasAnomalias));

            await GuardarParametroAsync(
                "IA_DIAS_HISTORICO_PRONOSTICO",
                DiasHistoricoPronostico!
                    .Value
                    .ToString());

            MensajeExito =
                "Configuración de inteligencia artificial guardada correctamente.";

            _logger.LogInformation(
                "Configuración IA guardada | " +
                "Usuario={IdUsuario} | " +
                "Empresa={IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return RedirectToPage(
                "/Configuracion/IA");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando configuración IA.");

            MensajeError =
                $"No se pudo guardar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // CARGAR CONFIGURACIÓN
    // =========================================================

    private async Task CargarConfiguracionAsync()
    {
        var parametros =
            await _parametroService
                .ListarAsync(
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

        IaActiva =
            ObtenerBool(
                Obtener(
                    "IA_ACTIVA",
                    "0"));

        IaPronosticoVentas =
            ObtenerBool(
                Obtener(
                    "IA_PRONOSTICO_VENTAS",
                    "0"));

        IaRecomendacionReposicion =
            ObtenerBool(
                Obtener(
                    "IA_RECOMENDACION_REPOSICION",
                    "0"));

        IaAlertasAnomalias =
            ObtenerBool(
                Obtener(
                    "IA_ALERTAS_ANOMALIAS",
                    "0"));

        DiasHistoricoPronostico =
            ObtenerEntero(
                Obtener(
                    "IA_DIAS_HISTORICO_PRONOSTICO",
                    "90"),
                90);
    }

    // =========================================================
    // GUARDAR PARAMETRO
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

        if (!DiasHistoricoPronostico.HasValue)
        {
            ModelState.AddModelError(
                nameof(DiasHistoricoPronostico),
                "Ingrese los días de historial.");
        }
        else if (
            DiasHistoricoPronostico.Value < 7 ||
            DiasHistoricoPronostico.Value > 3650)
        {
            ModelState.AddModelError(
                nameof(DiasHistoricoPronostico),
                "El historial debe estar entre 7 y 3650 días.");
        }

        // IMPORTANTE:
        // No apagar las otras opciones automáticamente.
    }

    // =========================================================
    // EMPRESA
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

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool ObtenerBool(
        string valor)
    {
        if (string.IsNullOrWhiteSpace(
            valor))
        {
            return false;
        }

        valor =
            valor.Trim();

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