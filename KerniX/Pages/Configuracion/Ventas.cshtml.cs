using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Services.Configuracion;
using KerniX.Models.Ventas.Dashboard;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace KerniX.Pages.Configuracion;

public sealed class VentasModel : PageModel
{
    private readonly IParametroSistemaDbService _parametroService;
    private readonly ILogger<VentasModel> _logger;

    public VentasModel(
        IParametroSistemaDbService parametroService,
        ILogger<VentasModel> logger)
    {
        _parametroService = parametroService;
        _logger = logger;
    }

    // =========================================================
    // PROPIEDADES
    // =========================================================
    public List<ClienteDestacadoResponse> TopClientes { get; set; } = new();

    public List<AlertaComercialResponse> AlertasComerciales { get; set; } = new();

    public List<ProyeccionVentaResponse> ProyeccionVentas { get; set; } = new();
    [BindProperty]
    public int IdEmpresa { get; set; }

    [BindProperty]
    public bool PermitirVentaSinStock { get; set; }

    [BindProperty]
    public bool ValidarStockAntesVenta { get; set; }

    [BindProperty]
    public bool PermitirCambioPrecio { get; set; }

    [BindProperty]
    public decimal? DescuentoMaximo { get; set; }

    [BindProperty]
    public bool PermitirVentaCredito { get; set; }

    [BindProperty]
    public decimal? LimiteCreditoDefault { get; set; }

    [BindProperty]
    public int? DiasCreditoDefault { get; set; }

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
                "Configuración Ventas cargada | " +
                "Usuario={IdUsuario} | " +
                "Empresa={IdEmpresa} | " +
                "VentaSinStock={VentaSinStock} | " +
                "ValidarStock={ValidarStock} | " +
                "CambioPrecio={CambioPrecio} | " +
                "Descuento={Descuento} | " +
                "Credito={Credito} | " +
                "LimiteCredito={LimiteCredito} | " +
                "DiasCredito={DiasCredito}",
                idUsuario.Value,
                IdEmpresa,
                PermitirVentaSinStock,
                ValidarStockAntesVenta,
                PermitirCambioPrecio,
                DescuentoMaximo,
                PermitirVentaCredito,
                LimiteCreditoDefault,
                DiasCreditoDefault);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración de ventas.");

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
            "VENTAS POST => " +
            "VentaSinStock={VentaSinStock} | " +
            "ValidarStock={ValidarStock} | " +
            "CambioPrecio={CambioPrecio} | " +
            "Descuento={Descuento} | " +
            "Credito={Credito} | " +
            "LimiteCredito={LimiteCredito} | " +
            "DiasCredito={DiasCredito}",
            PermitirVentaSinStock,
            ValidarStockAntesVenta,
            PermitirCambioPrecio,
            DescuentoMaximo,
            PermitirVentaCredito,
            LimiteCreditoDefault,
            DiasCreditoDefault);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await GuardarParametroAsync(
                "PERMITIR_VENTA_SIN_STOCK",
                BoolAValor(
                    PermitirVentaSinStock));

            await GuardarParametroAsync(
                "VALIDAR_STOCK_ANTES_VENTA",
                BoolAValor(
                    ValidarStockAntesVenta));

            await GuardarParametroAsync(
                "PERMITIR_CAMBIO_PRECIO",
                BoolAValor(
                    PermitirCambioPrecio));

            await GuardarParametroAsync(
                "DESCUENTO_MAXIMO",
                DecimalAValor(
                    DescuentoMaximo!.Value));

            await GuardarParametroAsync(
                "PERMITIR_VENTA_CREDITO",
                BoolAValor(
                    PermitirVentaCredito));

            await GuardarParametroAsync(
                "LIMITE_CREDITO_DEFAULT",
                DecimalAValor(
                    LimiteCreditoDefault!.Value));

            await GuardarParametroAsync(
                "DIAS_CREDITO_DEFAULT",
                DiasCreditoDefault!
                    .Value
                    .ToString(
                        CultureInfo.InvariantCulture));

            MensajeExito =
                "Configuración de ventas guardada correctamente.";

            _logger.LogInformation(
                "Configuración Ventas guardada | " +
                "Usuario={IdUsuario} | Empresa={IdEmpresa}",
                idUsuario.Value,
                IdEmpresa);

            return RedirectToPage(
                "/Configuracion/Ventas");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando configuración de ventas.");

            MensajeError =
                $"No se pudo guardar la configuración: {ex.Message}";

            return Page();
        }
    }

    // =========================================================
    // CARGAR CONFIGURACION
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

        PermitirVentaSinStock =
            ObtenerBool(
                Obtener(
                    "PERMITIR_VENTA_SIN_STOCK",
                    "0"));

        ValidarStockAntesVenta =
            ObtenerBool(
                Obtener(
                    "VALIDAR_STOCK_ANTES_VENTA",
                    "1"));

        PermitirCambioPrecio =
            ObtenerBool(
                Obtener(
                    "PERMITIR_CAMBIO_PRECIO",
                    "0"));

        DescuentoMaximo =
            ObtenerDecimal(
                Obtener(
                    "DESCUENTO_MAXIMO",
                    "20"),
                20m);

        PermitirVentaCredito =
            ObtenerBool(
                Obtener(
                    "PERMITIR_VENTA_CREDITO",
                    "1"));

        LimiteCreditoDefault =
            ObtenerDecimal(
                Obtener(
                    "LIMITE_CREDITO_DEFAULT",
                    "0"),
                0m);

        DiasCreditoDefault =
            ObtenerEntero(
                Obtener(
                    "DIAS_CREDITO_DEFAULT",
                    "30"),
                30);
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
                IdEmpresa =
                    IdEmpresa,

                Codigo =
                    codigo,

                Valor =
                    valor
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

        if (!DescuentoMaximo.HasValue)
        {
            ModelState.AddModelError(
                nameof(DescuentoMaximo),
                "Ingrese el descuento máximo.");
        }
        else if (
            DescuentoMaximo.Value < 0 ||
            DescuentoMaximo.Value > 100)
        {
            ModelState.AddModelError(
                nameof(DescuentoMaximo),
                "El descuento debe estar entre 0 y 100.");
        }

        if (!LimiteCreditoDefault.HasValue)
        {
            ModelState.AddModelError(
                nameof(LimiteCreditoDefault),
                "Ingrese el límite de crédito.");
        }
        else if (
            LimiteCreditoDefault.Value < 0)
        {
            ModelState.AddModelError(
                nameof(LimiteCreditoDefault),
                "El límite de crédito no puede ser negativo.");
        }

        if (!DiasCreditoDefault.HasValue)
        {
            ModelState.AddModelError(
                nameof(DiasCreditoDefault),
                "Ingrese los días de crédito.");
        }
        else if (
            DiasCreditoDefault.Value < 0 ||
            DiasCreditoDefault.Value > 3650)
        {
            ModelState.AddModelError(
                nameof(DiasCreditoDefault),
                "Los días de crédito deben estar entre 0 y 3650.");
        }
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
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int resultado)
                ? resultado
                : valorDefault;
    }

    private static decimal ObtenerDecimal(
        string valor,
        decimal valorDefault)
    {
        if (string.IsNullOrWhiteSpace(
            valor))
        {
            return valorDefault;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal resultado))
        {
            return resultado;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out resultado))
        {
            return resultado;
        }

        return valorDefault;
    }

    private static string DecimalAValor(
        decimal valor)
    {
        return valor.ToString(
            CultureInfo.InvariantCulture);
    }

    private static string BoolAValor(
        bool valor)
    {
        return valor
            ? "1"
            : "0";
    }
}