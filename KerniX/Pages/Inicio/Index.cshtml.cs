using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Inicio;

public sealed class IndexModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        TokenStorageService tokenStorage,
        ILogger<IndexModel> logger)
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


    public string Correo { get; private set; }
        = string.Empty;


    public int IdUsuario { get; private set; }


    public int IdEmpleado { get; private set; }


    // =========================================================
    // GET
    // =========================================================

    public IActionResult OnGet()
    {
        // =====================================================
        // VALIDAR AUTENTICACIÓN
        // =====================================================

        if (!_tokenStorage.EstaAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }


        // =====================================================
        // USUARIO
        // =====================================================

        Usuario =
            HttpContext.Session.GetString(
                SessionKeys.Usuario)
            ?? "Usuario";


        // =====================================================
        // NOMBRE
        // =====================================================

        NombreCompleto =
            HttpContext.Session.GetString(
                SessionKeys.NombreCompleto)
            ?? Usuario;


        // =====================================================
        // CARGO
        // =====================================================

        Cargo =
            HttpContext.Session.GetString(
                SessionKeys.Cargo)
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(Cargo))
        {
            Cargo =
                "Usuario KerniX";
        }


        // =====================================================
        // CORREO
        // =====================================================

        Correo =
            HttpContext.Session.GetString(
                SessionKeys.Correo)
            ?? string.Empty;


        // =====================================================
        // ID USUARIO
        // =====================================================

        IdUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario)
            ?? 0;


        // =====================================================
        // ID EMPLEADO
        // =====================================================

        IdEmpleado =
            HttpContext.Session.GetInt32(
                SessionKeys.IdEmpleado)
            ?? 0;


        return Page();
    }
}