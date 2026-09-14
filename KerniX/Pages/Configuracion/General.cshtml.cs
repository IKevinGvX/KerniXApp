using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class GeneralModel : PageModel
{
    private readonly IParametroSistemaDbService _parametroService;
    private readonly ILogger<GeneralModel> _logger;

    public GeneralModel(
        IParametroSistemaDbService parametroService,
        ILogger<GeneralModel> logger)
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
    // FORMULARIO
    // =========================================================

    [BindProperty]
    public string? MonedaPrincipal { get; set; }

    [BindProperty]
    public string? ZonaHoraria { get; set; }

    [BindProperty]
    public string? Idioma { get; set; }

    [BindProperty]
    public string? FormatoFecha { get; set; }

    [BindProperty]
    public string? FormatoHora { get; set; }

    [BindProperty]
    public int? DecimalesPrecio { get; set; }

    [BindProperty]
    public int? DecimalesCantidad { get; set; }

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

        int idEmpresaSesion =
            HttpContext.Session.GetInt32(
                "IdEmpresa") ?? 0;

        if (idEmpresaSesion <= 0)
        {
            idEmpresaSesion = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                idEmpresaSesion);
        }

        IdEmpresa =
            idEmpresaSesion;

        try
        {
            await CargarConfiguracionAsync();

            _logger.LogInformation(
                "Configuración General cargada. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración general. " +
                "IdEmpresa: {IdEmpresa}",
                IdEmpresa);

            MensajeError =
                $"No se pudo cargar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // POST - GUARDAR
    // =========================================================

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        _logger.LogInformation(
            "ENTRÓ A OnPostGuardarAsync | IdEmpresa recibido: {IdEmpresa}",
            IdEmpresa);

        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return RedirectToPage(
                "/Account/Login");
        }

        // =====================================================
        // EMPRESA
        // =====================================================

        int idEmpresaSesion =
            HttpContext.Session.GetInt32(
                "IdEmpresa") ?? 0;

        if (idEmpresaSesion > 0)
        {
            IdEmpresa =
                idEmpresaSesion;
        }
        else if (IdEmpresa > 0)
        {
            HttpContext.Session.SetInt32(
                "IdEmpresa",
                IdEmpresa);
        }
        else
        {
            IdEmpresa = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                IdEmpresa);
        }

        // =====================================================
        // NORMALIZAR VALORES
        // =====================================================

        MonedaPrincipal =
            MonedaPrincipal?.Trim();

        ZonaHoraria =
            ZonaHoraria?.Trim();

        Idioma =
            Idioma?.Trim();

        FormatoFecha =
            FormatoFecha?.Trim();

        FormatoHora =
            FormatoHora?.Trim();

        // =====================================================
        // VALIDAR
        // =====================================================

        ValidarFormulario();

        foreach (var item in ModelState)
        {
            foreach (var error in item.Value.Errors)
            {
                _logger.LogWarning(
                    "MODELSTATE ERROR | Campo: {Campo} | Error: {Error}",
                    item.Key,
                    error.ErrorMessage);
            }
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Configuración General inválida. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }

        // =====================================================
        // GUARDAR
        // =====================================================

        try
        {
            await GuardarParametroAsync(
                "MONEDA_PRINCIPAL",
                MonedaPrincipal!);

            await GuardarParametroAsync(
                "ZONA_HORARIA",
                ZonaHoraria!);

            await GuardarParametroAsync(
                "IDIOMA",
                Idioma!);

            await GuardarParametroAsync(
                "FORMATO_FECHA",
                FormatoFecha!);

            await GuardarParametroAsync(
                "FORMATO_HORA",
                FormatoHora!);

            await GuardarParametroAsync(
                "DECIMALES_PRECIO",
                DecimalesPrecio!.Value.ToString());

            await GuardarParametroAsync(
                "DECIMALES_CANTIDAD",
                DecimalesCantidad!.Value.ToString());

            _logger.LogInformation(
                "Configuración General guardada correctamente. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            MensajeExito =
                "Configuración general guardada correctamente.";

            return RedirectToPage(
                "/Configuracion/General");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando Configuración General. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

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
            await _parametroService.ListarAsync(
                IdEmpresa);

        string ObtenerValor(
            string codigo,
            string valorPredeterminado)
        {
            var parametro =
                parametros.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Codigo,
                            codigo,
                            StringComparison.OrdinalIgnoreCase));

            if (parametro is null)
            {
                return valorPredeterminado;
            }

            if (string.IsNullOrWhiteSpace(
                parametro.Valor))
            {
                return valorPredeterminado;
            }

            return parametro.Valor.Trim();
        }

        MonedaPrincipal =
            ObtenerValor(
                "MONEDA_PRINCIPAL",
                "PEN");

        ZonaHoraria =
            ObtenerValor(
                "ZONA_HORARIA",
                "America/Lima");

        Idioma =
            ObtenerValor(
                "IDIOMA",
                "es-PE");

        FormatoFecha =
            ObtenerValor(
                "FORMATO_FECHA",
                "dd/MM/yyyy");

        FormatoHora =
            ObtenerValor(
                "FORMATO_HORA",
                "HH:mm");

        string valorDecimalesPrecio =
            ObtenerValor(
                "DECIMALES_PRECIO",
                "2");

        if (!int.TryParse(
            valorDecimalesPrecio,
            out int decimalesPrecio))
        {
            decimalesPrecio = 2;
        }

        DecimalesPrecio =
            decimalesPrecio;

        string valorDecimalesCantidad =
            ObtenerValor(
                "DECIMALES_CANTIDAD",
                "2");

        if (!int.TryParse(
            valorDecimalesCantidad,
            out int decimalesCantidad))
        {
            decimalesCantidad = 2;
        }

        DecimalesCantidad =
            decimalesCantidad;
    }

    // =========================================================
    // GUARDAR PARÁMETRO
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
        ModelState.Clear();

        if (IdEmpresa <= 0)
        {
            ModelState.AddModelError(
                nameof(IdEmpresa),
                "No se pudo determinar la empresa.");
        }

        if (string.IsNullOrWhiteSpace(
            MonedaPrincipal))
        {
            ModelState.AddModelError(
                nameof(MonedaPrincipal),
                "Seleccione una moneda.");
        }

        if (string.IsNullOrWhiteSpace(
            ZonaHoraria))
        {
            ModelState.AddModelError(
                nameof(ZonaHoraria),
                "Seleccione una zona horaria.");
        }

        if (string.IsNullOrWhiteSpace(
            Idioma))
        {
            ModelState.AddModelError(
                nameof(Idioma),
                "Seleccione un idioma.");
        }

        if (string.IsNullOrWhiteSpace(
            FormatoFecha))
        {
            ModelState.AddModelError(
                nameof(FormatoFecha),
                "Seleccione un formato de fecha.");
        }

        if (string.IsNullOrWhiteSpace(
            FormatoHora))
        {
            ModelState.AddModelError(
                nameof(FormatoHora),
                "Seleccione un formato de hora.");
        }

        if (!DecimalesPrecio.HasValue)
        {
            ModelState.AddModelError(
                nameof(DecimalesPrecio),
                "Ingrese los decimales para precios.");
        }
        else if (
            DecimalesPrecio.Value < 0 ||
            DecimalesPrecio.Value > 6)
        {
            ModelState.AddModelError(
                nameof(DecimalesPrecio),
                "Los decimales de precio deben estar entre 0 y 6.");
        }

        if (!DecimalesCantidad.HasValue)
        {
            ModelState.AddModelError(
                nameof(DecimalesCantidad),
                "Ingrese los decimales para cantidades.");
        }
        else if (
            DecimalesCantidad.Value < 0 ||
            DecimalesCantidad.Value > 6)
        {
            ModelState.AddModelError(
                nameof(DecimalesCantidad),
                "Los decimales de cantidad deben estar entre 0 y 6.");
        }
    }
}