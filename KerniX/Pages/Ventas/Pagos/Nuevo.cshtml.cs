using KerniX.Models.Ventas.Operaciones;
using KerniX.Security;
using KerniX.Services.Ventas.Operaciones;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Ventas.Pagos;

public sealed class NuevoModel : PageModel
{
    private readonly IVentasRegistroService _registro;
    public NuevoModel(IVentasRegistroService registro) => _registro = registro;

    [BindProperty] public PagoRegistroInput Input { get; set; } = new();
    public string? Error { get; private set; }

    public IActionResult OnGet(long? idVenta)
    {
        if ((HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0) <= 0)
            return Redirect("/Account/Login");
        if (idVenta.GetValueOrDefault() > 0) Input.IdVenta = idVenta.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        int usuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        if (usuario <= 0) return Redirect("/Account/Login");

        if (!ModelState.IsValid)
        {
            Error = "Revisa los datos del formulario antes de continuar.";
            return Page();
        }
        var resultado = await _registro.RegistrarPagoAsync(usuario, Input, cancellationToken);
        if (!resultado.Correcto)
        {
            Error = resultado.Mensaje;
            return Page();
        }
        TempData["KxPagoOk"] = resultado.Mensaje;
        return RedirectToPage(new { idVenta = Input.IdVenta });
    }
}
