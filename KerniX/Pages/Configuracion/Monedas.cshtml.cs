using KerniX.Api.Models.Configuracion.Moneda;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class MonedasModel : PageModel
{
    private readonly IMonedaDbService _monedaService;
    private readonly ILogger<MonedasModel> _logger;

    public MonedasModel(
        IMonedaDbService monedaService,
        ILogger<MonedasModel> logger)
    {
        _monedaService = monedaService;
        _logger = logger;
    }

    public IReadOnlyList<MonedaResponse> Monedas { get; set; }
        = Array.Empty<MonedaResponse>();

    // =========================================================
    // CREAR
    // =========================================================

    [BindProperty]
    public string? NuevoCodigo { get; set; }

    [BindProperty]
    public string? NuevoNombre { get; set; }

    [BindProperty]
    public string? NuevoSimbolo { get; set; }

    [BindProperty]
    public int? NuevosDecimales { get; set; }

    [BindProperty]
    public bool NuevoEstado { get; set; } = true;

    // =========================================================
    // EDITAR
    // =========================================================

    [BindProperty]
    public int IdMonedaEditar { get; set; }

    [BindProperty]
    public string? CodigoEditar { get; set; }

    [BindProperty]
    public string? NombreEditar { get; set; }

    [BindProperty]
    public string? SimboloEditar { get; set; }

    [BindProperty]
    public int? DecimalesEditar { get; set; }

    [BindProperty]
    public bool EstadoEditar { get; set; }

    // =========================================================
    // ESTADO
    // =========================================================

    [BindProperty]
    public int IdMonedaEstado { get; set; }

    [BindProperty]
    public bool NuevoEstadoMoneda { get; set; }

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync()
    {
        if (!UsuarioAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        await CargarMonedasAsync();

        return Page();
    }

    // =========================================================
    // CREAR
    // =========================================================

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!UsuarioAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        NuevoCodigo =
            NuevoCodigo?
                .Trim()
                .ToUpperInvariant();

        NuevoNombre =
            NuevoNombre?.Trim();

        NuevoSimbolo =
            string.IsNullOrWhiteSpace(
                NuevoSimbolo)
                ? null
                : NuevoSimbolo.Trim();

        ValidarNuevaMoneda();

        if (!ModelState.IsValid)
        {
            await CargarMonedasAsync();

            return Page();
        }

        try
        {
            var existente =
                await _monedaService
                    .ObtenerPorCodigoAsync(
                        NuevoCodigo!);

            if (existente is not null)
            {
                MensajeError =
                    $"Ya existe una moneda con código {NuevoCodigo}.";

                await CargarMonedasAsync();

                return Page();
            }

            var request =
                new MonedaCrearRequest
                {
                    Codigo =
                        NuevoCodigo!,

                    Nombre =
                        NuevoNombre!,

                    Simbolo =
                        NuevoSimbolo,

                    CantidadDecimales =
                        NuevosDecimales!.Value,

                    Estado =
                        NuevoEstado
                };

            await _monedaService
                .CrearAsync(
                    request);

            MensajeExito =
                "Moneda creada correctamente.";

            return RedirectToPage(
                "/Configuracion/Monedas");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creando moneda.");

            MensajeError =
                ex.Message;

            await CargarMonedasAsync();

            return Page();
        }
    }

    // =========================================================
    // EDITAR
    // =========================================================

    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!UsuarioAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        CodigoEditar =
            CodigoEditar?
                .Trim()
                .ToUpperInvariant();

        NombreEditar =
            NombreEditar?.Trim();

        SimboloEditar =
            string.IsNullOrWhiteSpace(
                SimboloEditar)
                ? null
                : SimboloEditar.Trim();

        ValidarEdicion();

        if (!ModelState.IsValid)
        {
            await CargarMonedasAsync();

            return Page();
        }

        try
        {
            var existente =
                await _monedaService
                    .ObtenerPorCodigoAsync(
                        CodigoEditar!);

            if (existente is not null &&
                existente.IdMoneda != IdMonedaEditar)
            {
                MensajeError =
                    $"Ya existe otra moneda con código {CodigoEditar}.";

                await CargarMonedasAsync();

                return Page();
            }

            var request =
                new MonedaActualizarRequest
                {
                    Codigo =
                        CodigoEditar!,

                    Nombre =
                        NombreEditar!,

                    Simbolo =
                        SimboloEditar,

                    CantidadDecimales =
                        DecimalesEditar!.Value,

                    Estado =
                        EstadoEditar
                };

            await _monedaService
                .ActualizarAsync(
                    IdMonedaEditar,
                    request);

            MensajeExito =
                "Moneda actualizada correctamente.";

            return RedirectToPage(
                "/Configuracion/Monedas");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando moneda {IdMoneda}.",
                IdMonedaEditar);

            MensajeError =
                ex.Message;

            await CargarMonedasAsync();

            return Page();
        }
    }

    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================

    public async Task<IActionResult> OnPostCambiarEstadoAsync()
    {
        if (!UsuarioAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        if (IdMonedaEstado <= 0)
        {
            MensajeError =
                "Moneda inválida.";

            return RedirectToPage(
                "/Configuracion/Monedas");
        }

        try
        {
            if (!NuevoEstadoMoneda)
            {
                bool tieneDependencias =
                    await _monedaService
                        .TieneDependenciasAsync(
                            IdMonedaEstado);

                if (tieneDependencias)
                {
                    MensajeError =
                        "No se puede desactivar la moneda porque tiene dependencias registradas.";

                    return RedirectToPage(
                        "/Configuracion/Monedas");
                }
            }

            await _monedaService
                .CambiarEstadoAsync(
                    IdMonedaEstado,
                    NuevoEstadoMoneda);

            MensajeExito =
                NuevoEstadoMoneda
                    ? "Moneda activada correctamente."
                    : "Moneda desactivada correctamente.";

            return RedirectToPage(
                "/Configuracion/Monedas");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando estado de moneda {IdMoneda}.",
                IdMonedaEstado);

            MensajeError =
                ex.Message;

            return RedirectToPage(
                "/Configuracion/Monedas");
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task CargarMonedasAsync()
    {
        Monedas =
            await _monedaService
                .ListarAsync();
    }

    private void ValidarNuevaMoneda()
    {
        if (string.IsNullOrWhiteSpace(
            NuevoCodigo))
        {
            ModelState.AddModelError(
                nameof(NuevoCodigo),
                "Ingrese el código de la moneda.");
        }

        if (string.IsNullOrWhiteSpace(
            NuevoNombre))
        {
            ModelState.AddModelError(
                nameof(NuevoNombre),
                "Ingrese el nombre de la moneda.");
        }

        if (!NuevosDecimales.HasValue)
        {
            ModelState.AddModelError(
                nameof(NuevosDecimales),
                "Ingrese la cantidad de decimales.");
        }
        else if (
            NuevosDecimales.Value < 0 ||
            NuevosDecimales.Value > 6)
        {
            ModelState.AddModelError(
                nameof(NuevosDecimales),
                "Los decimales deben estar entre 0 y 6.");
        }
    }

    private void ValidarEdicion()
    {
        if (IdMonedaEditar <= 0)
        {
            ModelState.AddModelError(
                nameof(IdMonedaEditar),
                "Moneda inválida.");
        }

        if (string.IsNullOrWhiteSpace(
            CodigoEditar))
        {
            ModelState.AddModelError(
                nameof(CodigoEditar),
                "Ingrese el código.");
        }

        if (string.IsNullOrWhiteSpace(
            NombreEditar))
        {
            ModelState.AddModelError(
                nameof(NombreEditar),
                "Ingrese el nombre.");
        }

        if (!DecimalesEditar.HasValue)
        {
            ModelState.AddModelError(
                nameof(DecimalesEditar),
                "Ingrese la cantidad de decimales.");
        }
        else if (
            DecimalesEditar.Value < 0 ||
            DecimalesEditar.Value > 6)
        {
            ModelState.AddModelError(
                nameof(DecimalesEditar),
                "Los decimales deben estar entre 0 y 6.");
        }
    }

    private bool UsuarioAutenticado()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        return
            idUsuario.HasValue &&
            idUsuario.Value > 0;
    }
}