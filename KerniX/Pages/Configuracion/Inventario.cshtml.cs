using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class InventarioModel : PageModel
{
    private readonly IParametroSistemaDbService _parametroService;
    private readonly ILogger<InventarioModel> _logger;

    public InventarioModel(
        IParametroSistemaDbService parametroService,
        ILogger<InventarioModel> logger)
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
    // INVENTARIO
    // =========================================================

    [BindProperty]
    public int? StockMinimoDefault { get; set; }

    [BindProperty]
    public bool PermitirStockNegativo { get; set; }

    [BindProperty]
    public bool AlertaStockBajo { get; set; }

    [BindProperty]
    public bool AlertaStockAgotado { get; set; }

    [BindProperty]
    public bool ControlarLotes { get; set; }

    [BindProperty]
    public bool ControlarSeries { get; set; }

    [BindProperty]
    public bool ControlarVencimiento { get; set; }

    [BindProperty]
    public bool PermitirAjustesManuales { get; set; }

    [BindProperty]
    public string? MetodoValorizacion { get; set; }

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
                "Configuración Inventario cargada. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración de inventario.");

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

        // =====================================================
        // MODELSTATE
        // =====================================================

        ModelState.Clear();

        ValidarFormulario();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Configuración Inventario inválida. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return Page();
        }

        try
        {
            await GuardarParametroAsync(
                "STOCK_MINIMO_DEFAULT",
                StockMinimoDefault!.Value.ToString());

            await GuardarParametroAsync(
                "PERMITIR_STOCK_NEGATIVO",
                BoolAValor(PermitirStockNegativo));

            await GuardarParametroAsync(
                "ALERTA_STOCK_BAJO",
                BoolAValor(AlertaStockBajo));

            await GuardarParametroAsync(
                "ALERTA_STOCK_AGOTADO",
                BoolAValor(AlertaStockAgotado));

            await GuardarParametroAsync(
                "CONTROLAR_LOTES",
                BoolAValor(ControlarLotes));

            await GuardarParametroAsync(
                "CONTROLAR_SERIES",
                BoolAValor(ControlarSeries));

            await GuardarParametroAsync(
                "CONTROLAR_VENCIMIENTO",
                BoolAValor(ControlarVencimiento));

            await GuardarParametroAsync(
                "PERMITIR_AJUSTES_MANUALES",
                BoolAValor(PermitirAjustesManuales));

            await GuardarParametroAsync(
                "METODO_VALORIZACION",
                MetodoValorizacion!);

            MensajeExito =
                "Configuración de inventario guardada correctamente.";

            _logger.LogInformation(
                "Configuración Inventario guardada. " +
                "IdUsuario: {IdUsuario} | IdEmpresa: {IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return RedirectToPage(
                "/Configuracion/Inventario");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando configuración de inventario.");

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

        StockMinimoDefault =
            ObtenerEntero(
                Obtener(
                    "STOCK_MINIMO_DEFAULT",
                    "10"),
                10);

        PermitirStockNegativo =
            ObtenerBool(
                Obtener(
                    "PERMITIR_STOCK_NEGATIVO",
                    "0"));

        AlertaStockBajo =
            ObtenerBool(
                Obtener(
                    "ALERTA_STOCK_BAJO",
                    "1"));

        AlertaStockAgotado =
            ObtenerBool(
                Obtener(
                    "ALERTA_STOCK_AGOTADO",
                    "1"));

        ControlarLotes =
            ObtenerBool(
                Obtener(
                    "CONTROLAR_LOTES",
                    "0"));

        ControlarSeries =
            ObtenerBool(
                Obtener(
                    "CONTROLAR_SERIES",
                    "0"));

        ControlarVencimiento =
            ObtenerBool(
                Obtener(
                    "CONTROLAR_VENCIMIENTO",
                    "0"));

        PermitirAjustesManuales =
            ObtenerBool(
                Obtener(
                    "PERMITIR_AJUSTES_MANUALES",
                    "1"));

        MetodoValorizacion =
            Obtener(
                "METODO_VALORIZACION",
                "PROMEDIO");
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

        if (!StockMinimoDefault.HasValue)
        {
            ModelState.AddModelError(
                nameof(StockMinimoDefault),
                "Ingrese el stock mínimo.");
        }
        else if (
            StockMinimoDefault.Value < 0 ||
            StockMinimoDefault.Value > 999999)
        {
            ModelState.AddModelError(
                nameof(StockMinimoDefault),
                "El stock mínimo no es válido.");
        }

        if (string.IsNullOrWhiteSpace(
            MetodoValorizacion))
        {
            ModelState.AddModelError(
                nameof(MetodoValorizacion),
                "Seleccione un método de valorización.");
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