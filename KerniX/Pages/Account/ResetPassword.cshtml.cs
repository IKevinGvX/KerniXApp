using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KerniX.Services.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Account;

public sealed class ResetPasswordModel : PageModel
{
    private readonly IRecuperacionClaveService _recuperacion;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        IRecuperacionClaveService recuperacion,
        ILogger<ResetPasswordModel> logger)
    {
        _recuperacion = recuperacion;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public NuevaClaveInput Input { get; set; } = new();

    public bool EnlaceValido { get; private set; }
    public bool Actualizada { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        CabecerasSeguridad();
        try
        {
            EnlaceValido = await _recuperacion.TokenEsValidoAsync(Token, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al verificar enlace de recuperación.");
            ErrorMessage = "No se pudo verificar el enlace. Inténtalo más tarde.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        CabecerasSeguridad();
        if (!ModelState.IsValid)
        {
            EnlaceValido = true;
            return Page();
        }

        if (Encoding.UTF8.GetByteCount(Input.Password) > 72)
        {
            EnlaceValido = true;
            ModelState.AddModelError("Input.Password", "La contraseña no debe superar 72 bytes UTF-8.");
            return Page();
        }

        try
        {
            bool ok = await _recuperacion.RestablecerAsync(
                Token, Input.Password, cancellationToken);
            if (!ok)
            {
                ErrorMessage = "El enlace ha expirado o ya se utilizó. Solicita uno nuevo.";
                return Page();
            }
            Actualizada = true;
            Token = string.Empty;
            Input = new NuevaClaveInput();
            ModelState.Clear();
            return Page();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al restablecer contraseña.");
            ErrorMessage = "No fue posible cambiar la contraseña. Inténtalo más tarde.";
            return Page();
        }
    }

    private void CabecerasSeguridad()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    public sealed class NuevaClaveInput
    {
        [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
        [MinLength(12, ErrorMessage = "Usa por lo menos 12 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repite la nueva contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
