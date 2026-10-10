using KerniX.Models.Proveedores;
using KerniX.Services.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Proveedores;

public sealed class EditarModel : PageModel
{
    private readonly IProveedorDbService _proveedores;
    private readonly ProveedorContextService _seguridad;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(IProveedorDbService proveedores,
        ProveedorContextService seguridad, ILogger<EditarModel> logger)
    {
        _proveedores = proveedores;
        _seguridad = seguridad;
        _logger = logger;
    }

    [BindProperty] public ProveedorFormulario Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    // Soporta /Inventario/Proveedores/Editar/3 y /Inventario/Proveedores/Editar?id=3.
    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        // No depender exclusivamente de la propiedad [BindProperty(SupportsGet = true)].
        if (id.HasValue) Id = id.Value;

        _logger.LogInformation(
            "[PROVEEDORES] Editar GET solicitado. Ruta={Ruta} Id={Id}",
            HttpContext.Request.Path.Value, Id);

        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        if (!ctx.PuedeGestionar) return StatusCode(403);
        if (Id <= 0)
        {
            _logger.LogWarning("[PROVEEDORES] Editar GET sin ID válido. Ruta={Ruta}",
                HttpContext.Request.Path.Value);
            return NotFound();
        }

        try
        {
            var proveedor = await _proveedores.ObtenerAsync(ctx.IdEmpresa, Id, cancellationToken);
            if (proveedor is null)
            {
                _logger.LogWarning(
                    "[PROVEEDORES] Editar GET sin proveedor para Id={Id}, Empresa={Empresa}",
                    Id, ctx.IdEmpresa);
                return NotFound();
            }
            Input = new ProveedorFormulario
            {
                TipoDocumento = proveedor.TipoDocumento,
                NumeroDocumento = proveedor.NumeroDocumento,
                RazonSocial = proveedor.RazonSocial,
                NombreComercial = proveedor.NombreComercial,
                Telefono = proveedor.Telefono,
                Correo = proveedor.Correo
            };
            _logger.LogInformation(
                "[PROVEEDORES] Editar GET: Id={Id} Empresa={Empresa} Documento={DocumentoCargado} Razon={RazonCargada}",
                Id, ctx.IdEmpresa, !string.IsNullOrWhiteSpace(Input.NumeroDocumento),
                !string.IsNullOrWhiteSpace(Input.RazonSocial));
            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error leyendo proveedor Id={Id}", Id);
            return StatusCode(500);
        }
    }

    public async Task<IActionResult> OnPostAsync(int? id, CancellationToken cancellationToken)
    {
        if (id.HasValue) Id = id.Value;
        var ctx = await _seguridad.ObtenerAsync(cancellationToken);
        if (ctx is null) return RedirectToPage("/Account/Login");
        if (!ctx.PuedeGestionar) return StatusCode(403);
        if (Id <= 0) return BadRequest();
        _logger.LogInformation("[PROVEEDORES] Editar POST recibido. Id={Id}, Empresa={Empresa}, DocumentoPresente={DocumentoPresente}",
            Id, ctx.IdEmpresa, !string.IsNullOrWhiteSpace(Input.NumeroDocumento));
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[PROVEEDORES] Editar POST inválido: {Errores}",
                string.Join(" | ", ModelState.SelectMany(x => x.Value?.Errors
                    .Select(e => x.Key + ": " + e.ErrorMessage) ?? Enumerable.Empty<string>())));
            return Page();
        }
        try
        {
            bool ok = await _proveedores.ActualizarAsync(ctx.IdEmpresa, ctx.IdUsuario, Id, Input, cancellationToken);
            if (!ok) return NotFound();
            TempData["MensajeOk"] = "Información del proveedor actualizada correctamente.";
            return RedirectToPage("/Inventario/Proveedores/Detalle", new { id = Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[PROVEEDORES] Edición rechazada Id={Id}", Id);
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[PROVEEDORES] Error SQL editando Id={Id}", Id);
            ModelState.AddModelError(string.Empty, ex.Number is 2601 or 2627
                ? "Los datos ya pertenecen a otro proveedor."
                : ex.Number == 208
                    ? "Falta una tabla requerida (por ejemplo, com.proveedor_auditoria_web). Ejecuta el script SQL de auditoría."
                    : "No se pudo actualizar. Revisa las restricciones de SQL Server.");
        }
        return Page();
    }
}
