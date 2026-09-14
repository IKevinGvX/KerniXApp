using KerniX.Api.Models.Configuracion.SerieComprobante;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class SeriesModel : PageModel
{
    private readonly ISerieComprobanteDbService _serieService;
    private readonly ILogger<SeriesModel> _logger;

    public SeriesModel(
        ISerieComprobanteDbService serieService,
        ILogger<SeriesModel> logger)
    {
        _serieService = serieService;
        _logger = logger;
    }

    // =========================================================
    // CONTEXTO
    // =========================================================

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }

    // =========================================================
    // LISTADO
    // =========================================================

    public IReadOnlyList<SerieComprobanteResponse> Series { get; set; }
        = Array.Empty<SerieComprobanteResponse>();

    // =========================================================
    // CREAR
    // =========================================================

    [BindProperty]
    public string? NuevoTipoComprobante { get; set; }

    [BindProperty]
    public string? NuevaSerie { get; set; }

    [BindProperty]
    public long? NuevoCorrelativo { get; set; }

    [BindProperty]
    public bool NuevaSerieEstado { get; set; } = true;

    // =========================================================
    // EDITAR
    // =========================================================

    [BindProperty]
    public int IdSerieEditar { get; set; }

    [BindProperty]
    public string? TipoComprobanteEditar { get; set; }

    [BindProperty]
    public string? SerieEditar { get; set; }

    [BindProperty]
    public bool EstadoEditar { get; set; }

    // =========================================================
    // CORRELATIVO
    // =========================================================

    [BindProperty]
    public int IdSerieCorrelativo { get; set; }

    [BindProperty]
    public long? CorrelativoEditar { get; set; }

    // =========================================================
    // ESTADO
    // =========================================================

    [BindProperty]
    public int IdSerieEstado { get; set; }

    [BindProperty]
    public bool NuevoEstado { get; set; }

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
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        await CargarSeriesAsync();

        return Page();
    }

    // =========================================================
    // CREAR
    // =========================================================

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        NuevoTipoComprobante =
            NuevoTipoComprobante?.Trim().ToUpperInvariant();

        NuevaSerie =
            NuevaSerie?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(
            NuevoTipoComprobante))
        {
            ModelState.AddModelError(
                nameof(NuevoTipoComprobante),
                "Seleccione el tipo de comprobante.");
        }

        if (string.IsNullOrWhiteSpace(
            NuevaSerie))
        {
            ModelState.AddModelError(
                nameof(NuevaSerie),
                "Ingrese la serie.");
        }

        if (!NuevoCorrelativo.HasValue)
        {
            ModelState.AddModelError(
                nameof(NuevoCorrelativo),
                "Ingrese el correlativo inicial.");
        }
        else if (NuevoCorrelativo.Value < 0)
        {
            ModelState.AddModelError(
                nameof(NuevoCorrelativo),
                "El correlativo no puede ser negativo.");
        }

        if (!ModelState.IsValid)
        {
            await CargarSeriesAsync();

            return Page();
        }

        try
        {
            var request =
                new SerieComprobanteCrearRequest
                {
                    IdEmpresa = IdEmpresa,
                    IdSucursal = IdSucursal,
                    TipoComprobante = NuevoTipoComprobante!,
                    Serie = NuevaSerie!,
                    CorrelativoActual = NuevoCorrelativo!.Value,
                    Estado = NuevaSerieEstado
                };

            await _serieService.CrearAsync(
                request);

            MensajeExito =
                "Serie creada correctamente.";

            _logger.LogInformation(
                "Serie creada. Empresa: {IdEmpresa} | Sucursal: {IdSucursal} | Tipo: {Tipo} | Serie: {Serie}",
                IdEmpresa,
                IdSucursal,
                NuevoTipoComprobante,
                NuevaSerie);

            return RedirectToPage(
                "/Configuracion/Series");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creando serie.");

            MensajeError =
                ex.Message;

            await CargarSeriesAsync();

            return Page();
        }
    }

    // =========================================================
    // EDITAR
    // =========================================================

    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        TipoComprobanteEditar =
            TipoComprobanteEditar?
                .Trim()
                .ToUpperInvariant();

        SerieEditar =
            SerieEditar?
                .Trim()
                .ToUpperInvariant();

        if (IdSerieEditar <= 0)
        {
            ModelState.AddModelError(
                nameof(IdSerieEditar),
                "Serie inválida.");
        }

        if (string.IsNullOrWhiteSpace(
            TipoComprobanteEditar))
        {
            ModelState.AddModelError(
                nameof(TipoComprobanteEditar),
                "Seleccione el tipo de comprobante.");
        }

        if (string.IsNullOrWhiteSpace(
            SerieEditar))
        {
            ModelState.AddModelError(
                nameof(SerieEditar),
                "Ingrese la serie.");
        }

        if (!ModelState.IsValid)
        {
            await CargarSeriesAsync();

            return Page();
        }

        try
        {
            var request =
                new SerieComprobanteActualizarRequest
                {
                    TipoComprobante =
                        TipoComprobanteEditar!,

                    Serie =
                        SerieEditar!,

                    Estado =
                        EstadoEditar
                };

            await _serieService
                .ActualizarAsync(
                    IdSerieEditar,
                    request);

            MensajeExito =
                "Serie actualizada correctamente.";

            _logger.LogInformation(
                "Serie actualizada. IdSerie: {IdSerie} | Tipo: {Tipo} | Serie: {Serie}",
                IdSerieEditar,
                TipoComprobanteEditar,
                SerieEditar);

            return RedirectToPage(
                "/Configuracion/Series");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando serie {IdSerie}.",
                IdSerieEditar);

            MensajeError =
                ex.Message;

            await CargarSeriesAsync();

            return Page();
        }
    }

    // =========================================================
    // ACTUALIZAR CORRELATIVO
    // =========================================================

    public async Task<IActionResult>
        OnPostActualizarCorrelativoAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        if (IdSerieCorrelativo <= 0)
        {
            ModelState.AddModelError(
                nameof(IdSerieCorrelativo),
                "Serie inválida.");
        }

        if (!CorrelativoEditar.HasValue)
        {
            ModelState.AddModelError(
                nameof(CorrelativoEditar),
                "Ingrese el nuevo correlativo.");
        }
        else if (CorrelativoEditar.Value < 0)
        {
            ModelState.AddModelError(
                nameof(CorrelativoEditar),
                "El correlativo no puede ser negativo.");
        }

        if (!ModelState.IsValid)
        {
            await CargarSeriesAsync();

            return Page();
        }

        try
        {
            await _serieService
                .ActualizarCorrelativoAsync(
                    IdSerieCorrelativo,
                    CorrelativoEditar!.Value);

            MensajeExito =
                "Correlativo actualizado correctamente.";

            return RedirectToPage(
                "/Configuracion/Series");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando correlativo de serie {IdSerie}.",
                IdSerieCorrelativo);

            MensajeError =
                ex.Message;

            await CargarSeriesAsync();

            return Page();
        }
    }

    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================

    public async Task<IActionResult>
        OnPostCambiarEstadoAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        if (IdSerieEstado <= 0)
        {
            MensajeError =
                "Serie inválida.";

            return RedirectToPage(
                "/Configuracion/Series");
        }

        try
        {
            await _serieService
                .CambiarEstadoAsync(
                    IdSerieEstado,
                    NuevoEstado);

            MensajeExito =
                NuevoEstado
                    ? "Serie activada correctamente."
                    : "Serie desactivada correctamente.";

            return RedirectToPage(
                "/Configuracion/Series");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando estado de serie {IdSerie}.",
                IdSerieEstado);

            MensajeError =
                ex.Message;

            return RedirectToPage(
                "/Configuracion/Series");
        }
    }

    // =========================================================
    // CARGAR
    // =========================================================

    private async Task CargarSeriesAsync()
    {
        Series =
            await _serieService.ListarAsync(
                IdEmpresa,
                IdSucursal);
    }

    // =========================================================
    // CONTEXTO
    // =========================================================

    private bool ObtenerContexto()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return false;
        }

        IdEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa") ?? 0;

        IdSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal") ?? 0;

        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                IdEmpresa);
        }

        if (IdSucursal <= 0)
        {
            IdSucursal = 1;

            HttpContext.Session.SetInt32(
                "IdSucursal",
                IdSucursal);
        }

        return true;
    }
}