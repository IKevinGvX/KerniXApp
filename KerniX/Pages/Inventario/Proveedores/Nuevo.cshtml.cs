using KerniX.Models.Proveedores;
using KerniX.Services.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Proveedores;

public sealed class NuevoModel : PageModel
{
    private readonly IProveedorDbService _proveedores;
    private readonly ProveedorContextService _seguridad;
    private readonly ILogger<NuevoModel> _logger;

    public NuevoModel(IProveedorDbService proveedores,
        ProveedorContextService seguridad, ILogger<NuevoModel> logger)
    {
        _proveedores = proveedores;
        _seguridad = seguridad;
        _logger = logger;
    }

    [BindProperty] public ProveedorFormulario Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        return ctx.PuedeGestionar ? Page() : StatusCode(403);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        if (!ctx.PuedeGestionar) return StatusCode(403);
        _logger.LogInformation("[PROVEEDORES] Nuevo POST recibido. Empresa={Empresa}, DocumentoPresente={DocumentoPresente}",
            ctx.IdEmpresa, !string.IsNullOrWhiteSpace(Input.NumeroDocumento));
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[PROVEEDORES] Nuevo POST inválido: {Errores}",
                string.Join(" | ", ModelState.SelectMany(x => x.Value?.Errors
                    .Select(e => x.Key + ": " + e.ErrorMessage) ?? Enumerable.Empty<string>())));
            return Page();
        }
        try
        {
            int id = await _proveedores.CrearAsync(ctx.IdEmpresa, ctx.IdUsuario, Input, cancellationToken);
            TempData["MensajeOk"] = $"Proveedor #{id} registrado correctamente.";
            return RedirectToPage("/Inventario/Proveedores/Index");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[PROVEEDORES] Registro rechazado.");
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error SQL registrando proveedor.");
            ModelState.AddModelError(string.Empty, ex.Number is 2601 or 2627
                ? "Ya existe un proveedor con estos datos únicos."
                : ex.Number == 208
                    ? "Falta una tabla requerida (por ejemplo, com.proveedor_auditoria_web). Ejecuta el script SQL de auditoría."
                    : "No se pudo registrar. Revisa restricciones y procedimientos de la tabla de proveedores.");
        }
        return Page();
    }
}
