using KerniX.Api.Models.Configuracion.Impuesto;
using KerniX.Api.Services.Configuracion;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class ImpuestosModel : PageModel
{
    private readonly IImpuestoDbService _impuestoService;
    private readonly ILogger<ImpuestosModel> _logger;

    public ImpuestosModel(
        IImpuestoDbService impuestoService,
        ILogger<ImpuestosModel> logger)
    {
        _impuestoService = impuestoService;
        _logger = logger;
    }

    public int IdEmpresa { get; private set; }

    public IReadOnlyList<ImpuestoResponse> Impuestos { get; set; }
        = Array.Empty<ImpuestoResponse>();

    [BindProperty]
    public string? NuevoCodigo { get; set; }

    [BindProperty]
    public string? NuevoNombre { get; set; }

    [BindProperty]
    public decimal? NuevoPorcentaje { get; set; }

    [BindProperty]
    public DateTime? NuevaFechaInicio { get; set; }

    [BindProperty]
    public DateTime? NuevaFechaFin { get; set; }

    [BindProperty]
    public bool NuevoEstado { get; set; } = true;

    [BindProperty]
    public int IdImpuestoEditar { get; set; }

    [BindProperty]
    public string? NombreEditar { get; set; }

    [BindProperty]
    public decimal? PorcentajeEditar { get; set; }

    [BindProperty]
    public DateTime? FechaInicioEditar { get; set; }

    [BindProperty]
    public DateTime? FechaFinEditar { get; set; }

    [BindProperty]
    public bool EstadoEditar { get; set; }

    [BindProperty]
    public int IdImpuestoEstado { get; set; }

    [BindProperty]
    public bool EstadoNuevo { get; set; }

    [BindProperty]
    public int IdImpuestoCerrar { get; set; }

    [BindProperty]
    public DateTime? FechaFinCerrar { get; set; }

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        await CargarImpuestosAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!ObtenerContexto())
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

        if (string.IsNullOrWhiteSpace(
            NuevoCodigo))
        {
            ModelState.AddModelError(
                nameof(NuevoCodigo),
                "Ingrese el código.");
        }

        if (string.IsNullOrWhiteSpace(
            NuevoNombre))
        {
            ModelState.AddModelError(
                nameof(NuevoNombre),
                "Ingrese el nombre.");
        }

        if (!NuevoPorcentaje.HasValue)
        {
            ModelState.AddModelError(
                nameof(NuevoPorcentaje),
                "Ingrese el porcentaje.");
        }
        else if (
            NuevoPorcentaje.Value < 0 ||
            NuevoPorcentaje.Value > 100)
        {
            ModelState.AddModelError(
                nameof(NuevoPorcentaje),
                "El porcentaje debe estar entre 0 y 100.");
        }

        if (!NuevaFechaInicio.HasValue)
        {
            ModelState.AddModelError(
                nameof(NuevaFechaInicio),
                "Ingrese la fecha de inicio.");
        }

        if (
            NuevaFechaFin.HasValue &&
            NuevaFechaInicio.HasValue &&
            NuevaFechaFin.Value.Date <
            NuevaFechaInicio.Value.Date)
        {
            ModelState.AddModelError(
                nameof(NuevaFechaFin),
                "La fecha fin no puede ser menor que la fecha inicio.");
        }

        if (!ModelState.IsValid)
        {
            await CargarImpuestosAsync();

            return Page();
        }

        try
        {
            var existente =
                await _impuestoService
                    .ObtenerPorCodigoAsync(
                        IdEmpresa,
                        NuevoCodigo!);

            if (existente is not null)
            {
                MensajeError =
                    $"Ya existe un impuesto con código {NuevoCodigo}.";

                await CargarImpuestosAsync();

                return Page();
            }

            var request =
                new ImpuestoCrearRequest
                {
                    IdEmpresa =
                        IdEmpresa,

                    Codigo =
                        NuevoCodigo!,

                    Nombre =
                        NuevoNombre!,

                    Porcentaje =
                        NuevoPorcentaje!.Value,

                    FechaInicio =
                        NuevaFechaInicio!.Value.Date,

                    FechaFin =
                        NuevaFechaFin?.Date,

                    Estado =
                        NuevoEstado
                };

            await _impuestoService
                .CrearAsync(
                    request);

            MensajeExito =
                "Impuesto creado correctamente.";

            _logger.LogInformation(
                "Impuesto creado. Empresa: {IdEmpresa} | Código: {Codigo}",
                IdEmpresa,
                NuevoCodigo);

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creando impuesto.");

            MensajeError =
                ex.Message;

            await CargarImpuestosAsync();

            return Page();
        }
    }

    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        ModelState.Clear();

        NombreEditar =
            NombreEditar?.Trim();

        if (IdImpuestoEditar <= 0)
        {
            ModelState.AddModelError(
                nameof(IdImpuestoEditar),
                "Impuesto inválido.");
        }

        if (string.IsNullOrWhiteSpace(
            NombreEditar))
        {
            ModelState.AddModelError(
                nameof(NombreEditar),
                "Ingrese el nombre.");
        }

        if (!PorcentajeEditar.HasValue)
        {
            ModelState.AddModelError(
                nameof(PorcentajeEditar),
                "Ingrese el porcentaje.");
        }
        else if (
            PorcentajeEditar.Value < 0 ||
            PorcentajeEditar.Value > 100)
        {
            ModelState.AddModelError(
                nameof(PorcentajeEditar),
                "El porcentaje debe estar entre 0 y 100.");
        }

        if (!FechaInicioEditar.HasValue)
        {
            ModelState.AddModelError(
                nameof(FechaInicioEditar),
                "Ingrese la fecha de inicio.");
        }

        if (
            FechaFinEditar.HasValue &&
            FechaInicioEditar.HasValue &&
            FechaFinEditar.Value.Date <
            FechaInicioEditar.Value.Date)
        {
            ModelState.AddModelError(
                nameof(FechaFinEditar),
                "La fecha fin no puede ser menor que la fecha inicio.");
        }

        if (!ModelState.IsValid)
        {
            await CargarImpuestosAsync();

            return Page();
        }

        try
        {
            var request =
                new ImpuestoActualizarRequest
                {
                    Nombre =
                        NombreEditar!,

                    Porcentaje =
                        PorcentajeEditar!.Value,

                    FechaInicio =
                        FechaInicioEditar!.Value.Date,

                    FechaFin =
                        FechaFinEditar?.Date,

                    Estado =
                        EstadoEditar
                };

            await _impuestoService
                .ActualizarAsync(
                    IdImpuestoEditar,
                    request);

            MensajeExito =
                "Impuesto actualizado correctamente.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando impuesto {IdImpuesto}.",
                IdImpuestoEditar);

            MensajeError =
                ex.Message;

            await CargarImpuestosAsync();

            return Page();
        }
    }

    public async Task<IActionResult> OnPostCambiarEstadoAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        if (IdImpuestoEstado <= 0)
        {
            MensajeError =
                "Impuesto inválido.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }

        try
        {
            await _impuestoService
                .CambiarEstadoAsync(
                    IdImpuestoEstado,
                    EstadoNuevo);

            MensajeExito =
                EstadoNuevo
                    ? "Impuesto activado correctamente."
                    : "Impuesto desactivado correctamente.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando estado del impuesto {IdImpuesto}.",
                IdImpuestoEstado);

            MensajeError =
                ex.Message;

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
    }

    public async Task<IActionResult> OnPostCerrarVigenciaAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        if (IdImpuestoCerrar <= 0)
        {
            MensajeError =
                "Impuesto inválido.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }

        if (!FechaFinCerrar.HasValue)
        {
            MensajeError =
                "Ingrese la fecha de cierre.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }

        try
        {
            await _impuestoService
                .CerrarVigenciaAsync(
                    IdImpuestoCerrar,
                    FechaFinCerrar.Value.Date);

            MensajeExito =
                "Vigencia cerrada correctamente.";

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cerrando vigencia del impuesto {IdImpuesto}.",
                IdImpuestoCerrar);

            MensajeError =
                ex.Message;

            return RedirectToPage(
                "/Configuracion/Impuestos");
        }
    }

    private async Task CargarImpuestosAsync()
    {
        Impuestos =
            await _impuestoService
                .ListarAsync(
                    IdEmpresa);
    }

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

        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                IdEmpresa);
        }

        return true;
    }
}