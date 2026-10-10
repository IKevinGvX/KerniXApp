using KerniX.Models.Ventas.Comprobante;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Ventas;

public sealed class ComprobanteDetallePageModel
    : PageModel
{
    private readonly IComprobanteDbService
        _comprobanteDbService;

    public ComprobanteDetallePageModel(
        IComprobanteDbService comprobanteDbService)
    {
        _comprobanteDbService =
            comprobanteDbService;
    }

    public ComprobanteDetalleModel? Comprobante
    {
        get;
        private set;
    }

    public async Task<IActionResult>
        OnGetAsync(
            long idVenta,
            CancellationToken cancellationToken)
    {
        if (idVenta <= 0)
        {
            return BadRequest(
                "Venta inválida.");
        }

        Comprobante =
            await _comprobanteDbService
                .ObtenerDetalleAsync(
                    idVenta,
                    cancellationToken);

        if (Comprobante is null)
        {
            return NotFound();
        }

        return Page();
    }
}
