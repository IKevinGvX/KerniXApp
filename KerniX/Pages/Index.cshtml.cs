using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages;

public sealed class IndexModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;

    public IndexModel(
        TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public IActionResult OnGet()
    {
        // ==============================================
        // SIN SESIÓN
        // ==============================================

        if (!_tokenStorage.EstaAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        // ==============================================
        // CON SESIÓN
        // ==============================================

        return RedirectToPage(
            "/Inicio/Index");
    }
}