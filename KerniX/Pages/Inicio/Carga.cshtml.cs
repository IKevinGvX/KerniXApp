using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Inicio;

public sealed class CargaModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;
    private readonly ILogger<CargaModel> _logger;

    public CargaModel(
        TokenStorageService tokenStorage,
        ILogger<CargaModel> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    public string Usuario { get; private set; }
        = "Usuario";

    public string NombreCompleto { get; private set; }
        = "Usuario";

    public string Cargo { get; private set; }
        = "Usuario KerniX";

    public IActionResult OnGet()
    {
        if (!_tokenStorage.EstaAutenticado())
        {
            _logger.LogWarning(
                "Intento de acceso a /Inicio/Carga sin sesión.");

            return RedirectToPage(
                "/Account/Login");
        }

        Usuario =
            HttpContext.Session.GetString(
                SessionKeys.Usuario)
            ?? "Usuario";

        NombreCompleto =
            HttpContext.Session.GetString(
                SessionKeys.NombreCompleto)
            ?? Usuario;

        Cargo =
            HttpContext.Session.GetString(
                SessionKeys.Cargo)
            ?? "Usuario KerniX";

        if (string.IsNullOrWhiteSpace(Cargo))
        {
            Cargo = "Usuario KerniX";
        }

        _logger.LogInformation(
            "Carga iniciada para {NombreCompleto} - {Cargo}.",
            NombreCompleto,
            Cargo);

        return Page();
    }
}