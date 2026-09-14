using KerniX.Api.Models.Configuracion.Dashboard;
using KerniX.Api.Models.Configuracion.Impuesto;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using KerniX.Api.Models.Configuracion.Moneda;
using KerniX.Api.Models.Configuracion.ParametroSistema;
using KerniX.Api.Models.Configuracion.SerieComprobante;
using KerniX.Api.Services.Configuracion;
using KerniX.Models.Usuarios;
using KerniX.Security;
using KerniX.Services.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public class IndexModel : PageModel
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly IUsuarioService _usuarioService;

    private readonly IConfiguracionDbService
        _configuracionService;

    private readonly IParametroSistemaDbService
        _parametroService;

    private readonly IMonedaDbService
        _monedaService;

    private readonly IImpuestoDbService
        _impuestoService;

    private readonly ISerieComprobanteDbService
        _serieService;

    private readonly ICopiaSeguridadDbService
        _copiaService;

    private readonly IPlanMantenimientoDbService
        _planService;

    private readonly IAlertaMantenimientoDbService
        _alertaService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public IndexModel(
        IUsuarioService usuarioService,
        IConfiguracionDbService configuracionService,
        IParametroSistemaDbService parametroService,
        IMonedaDbService monedaService,
        IImpuestoDbService impuestoService,
        ISerieComprobanteDbService serieService,
        ICopiaSeguridadDbService copiaService,
        IPlanMantenimientoDbService planService,
        IAlertaMantenimientoDbService alertaService)
    {
        _usuarioService =
            usuarioService;

        _configuracionService =
            configuracionService;

        _parametroService =
            parametroService;

        _monedaService =
            monedaService;

        _impuestoService =
            impuestoService;

        _serieService =
            serieService;

        _copiaService =
            copiaService;

        _planService =
            planService;

        _alertaService =
            alertaService;
    }

    // =========================================================
    // CONTEXTO
    // =========================================================

    public int IdEmpresa
    {
        get;
        private set;
    }

    public int IdSucursal
    {
        get;
        private set;
    }

    // =========================================================
    // PERFIL
    // =========================================================

    public UsuarioPerfilResponse Perfil
    {
        get;
        private set;
    } = new();

    // =========================================================
    // CONFIGURACIÓN
    // =========================================================

    public ConfiguracionDiagnosticoResponse?
        Diagnostico
    {
        get;
        private set;
    }

    public ConfiguracionValidacionResponse?
        Validacion
    {
        get;
        private set;
    }

    public IReadOnlyList<ParametroSistemaResponse>
        Parametros
    {
        get;
        private set;
    } = Array.Empty<ParametroSistemaResponse>();

    public IReadOnlyList<MonedaResponse>
        Monedas
    {
        get;
        private set;
    } = Array.Empty<MonedaResponse>();

    public IReadOnlyList<ImpuestoResponse>
        Impuestos
    {
        get;
        private set;
    } = Array.Empty<ImpuestoResponse>();

    public IReadOnlyList<SerieComprobanteResponse>
        Series
    {
        get;
        private set;
    } = Array.Empty<SerieComprobanteResponse>();

    public IReadOnlyList<PlanMantenimientoResponse>
        Planes
    {
        get;
        private set;
    } = Array.Empty<PlanMantenimientoResponse>();

    public IReadOnlyList<AlertaMantenimientoResponse>
        Alertas
    {
        get;
        private set;
    } = Array.Empty<AlertaMantenimientoResponse>();

    public CopiaSeguridadResponse?
        UltimaCopia
    {
        get;
        private set;
    }

    // =========================================================
    // VALORES DE CONFIGURACIÓN
    // =========================================================

    public string MonedaPrincipal =>
        ObtenerValorParametro(
            "MONEDA_PRINCIPAL",
            "PEN");

    public string ZonaHoraria =>
        ObtenerValorParametro(
            "ZONA_HORARIA",
            "America/Lima");

    public string Idioma =>
        ObtenerValorParametro(
            "IDIOMA",
            "es-PE");

    public int DecimalesPrecio =>
        ObtenerEntero(
            "DECIMALES_PRECIO",
            2);

    public int DecimalesCantidad =>
        ObtenerEntero(
            "DECIMALES_CANTIDAD",
            2);

    public int StockMinimo =>
        ObtenerEntero(
            "STOCK_MINIMO_DEFAULT",
            10);

    public decimal DescuentoMaximo =>
        ObtenerDecimal(
            "DESCUENTO_MAXIMO",
            0);

    public bool PermitirStockNegativo =>
        ObtenerBooleano(
            "PERMITIR_STOCK_NEGATIVO");

    public bool AlertasStockActivas =>
        ObtenerBooleano(
            "ALERTA_STOCK_BAJO");

    public bool AlertasStockAgotado =>
        ObtenerBooleano(
            "ALERTA_STOCK_AGOTADO");

    public bool ControlarLotes =>
        ObtenerBooleano(
            "CONTROLAR_LOTES");

    public bool ControlarSeries =>
        ObtenerBooleano(
            "CONTROLAR_SERIES");

    public bool ControlarVencimiento =>
        ObtenerBooleano(
            "CONTROLAR_VENCIMIENTO");

    public bool PermitirVentaSinStock =>
        ObtenerBooleano(
            "PERMITIR_VENTA_SIN_STOCK");

    public bool PermitirCambioPrecio =>
        ObtenerBooleano(
            "PERMITIR_CAMBIO_PRECIO");

    public bool PermitirVentaCredito =>
        ObtenerBooleano(
            "PERMITIR_VENTA_CREDITO");

    public bool IaActiva =>
        ObtenerBooleano(
            "IA_ACTIVA");

    public bool IaPronosticoVentas =>
        ObtenerBooleano(
            "IA_PRONOSTICO_VENTAS");

    public bool IaReposicion =>
        ObtenerBooleano(
            "IA_RECOMENDACION_REPOSICION");

    public bool IaAnomalias =>
        ObtenerBooleano(
            "IA_ALERTAS_ANOMALIAS");

    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult>
        OnGetAsync()
    {
        // =====================================================
        // USUARIO
        // =====================================================

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
        // PERFIL
        // =====================================================

        UsuarioPerfilResponse? perfil =
            await _usuarioService
                .ObtenerPerfilAsync(
                    idUsuario.Value);

        if (perfil is null)
        {
            return RedirectToPage(
                "/Account/Login");
        }

        Perfil =
            perfil;

        // =====================================================
        // EMPRESA
        // =====================================================

        IdEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? 1;

        // =====================================================
        // SUCURSAL
        // =====================================================

        IdSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal")
            ?? 1;

        // =====================================================
        // PARÁMETROS
        // =====================================================

        Parametros =
            await _parametroService
                .ListarAsync(
                    IdEmpresa);

        // =====================================================
        // MONEDAS
        // =====================================================

        Monedas =
            await _monedaService
                .ListarActivasAsync();

        // =====================================================
        // IMPUESTOS
        // =====================================================

        Impuestos =
            await _impuestoService
                .ListarVigentesAsync(
                    IdEmpresa);

        // =====================================================
        // SERIES DE COMPROBANTES
        // =====================================================

        Series =
            await _serieService
                .ListarAsync(
                    IdEmpresa,
                    IdSucursal);

        // =====================================================
        // DIAGNÓSTICO
        // =====================================================

        Diagnostico =
            await _configuracionService
                .ObtenerDiagnosticoAsync(
                    IdEmpresa,
                    IdSucursal);

        // =====================================================
        // VALIDACIÓN
        // =====================================================

        Validacion =
            await _configuracionService
                .ValidarObligatoriosAsync(
                    IdEmpresa,
                    IdSucursal);

        // =====================================================
        // COPIA DE SEGURIDAD
        // =====================================================

        UltimaCopia =
            await _copiaService
                .ObtenerUltimaAsync(
                    IdEmpresa);

        // =====================================================
        // PLANES DE MANTENIMIENTO
        // =====================================================

        Planes =
            await _planService
                .ListarActivosAsync();

        // =====================================================
        // ALERTAS
        // =====================================================

        Alertas =
            await _alertaService
                .ListarPendientesAsync();

        return Page();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public string ObtenerValorParametro(
        string codigo,
        string valorDefault = "")
    {
        ParametroSistemaResponse? parametro =
            Parametros.FirstOrDefault(
                x =>
                    string.Equals(
                        x.Codigo,
                        codigo,
                        StringComparison.OrdinalIgnoreCase));

        if (parametro is null)
        {
            return valorDefault;
        }

        if (string.IsNullOrWhiteSpace(
            parametro.Valor))
        {
            return valorDefault;
        }

        return parametro.Valor;
    }

    private bool ObtenerBooleano(
        string codigo)
    {
        string valor =
            ObtenerValorParametro(
                codigo,
                "0");

        return
            valor == "1"
            ||
            valor.Equals(
                "true",
                StringComparison.OrdinalIgnoreCase)
            ||
            valor.Equals(
                "si",
                StringComparison.OrdinalIgnoreCase)
            ||
            valor.Equals(
                "sí",
                StringComparison.OrdinalIgnoreCase);
    }

    private int ObtenerEntero(
        string codigo,
        int valorDefault)
    {
        string valor =
            ObtenerValorParametro(
                codigo,
                valorDefault.ToString());

        if (int.TryParse(
            valor,
            out int resultado))
        {
            return resultado;
        }

        return valorDefault;
    }

    private decimal ObtenerDecimal(
        string codigo,
        decimal valorDefault)
    {
        string valor =
            ObtenerValorParametro(
                codigo,
                valorDefault.ToString());

        if (decimal.TryParse(
            valor,
            out decimal resultado))
        {
            return resultado;
        }

        return valorDefault;
    }
}