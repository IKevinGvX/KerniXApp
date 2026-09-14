using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        TokenStorageService tokenStorage,
        ILogger<LogoutModel> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
    }


    // =========================================================
    // GET
    // =========================================================

    public IActionResult OnGet()
    {
        return RedirectToPage(
            "/Account/Login");
    }


    // =========================================================
    // POST
    // =========================================================

    public IActionResult OnPost()
    {
        try
        {
            _logger.LogInformation(
                "Iniciando cierre de sesión.");


            // =====================================================
            // TOKENS
            // =====================================================

            _tokenStorage.EliminarTokens();


            // =====================================================
            // TODA LA SESIÓN
            // =====================================================

            HttpContext.Session.Clear();


            _logger.LogInformation(
                "Sesión cerrada correctamente.");


            // =====================================================
            // LOGIN
            // =====================================================

            return RedirectToPage(
                "/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al cerrar la sesión.");

            HttpContext.Session.Clear();

            return RedirectToPage(
                "/Account/Login");
        }
    }
}