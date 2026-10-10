using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using KerniX.Services.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Account;

public sealed class ForgotPasswordModel : PageModel
{
    private readonly IRecuperacionClaveService _recuperacion;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        IRecuperacionClaveService recuperacion,
        ILogger<ForgotPasswordModel> logger)
    {
        _recuperacion = recuperacion;
        _logger = logger;
    }

    [BindProperty]
    public SolicitudInput Input { get; set; } = new();

    public bool SolicitudRecibida { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() => CabecerasSeguridad();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        CabecerasSeguridad();
        if (!ModelState.IsValid) return Page();

        try
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "sin-ip";
            await _recuperacion.SolicitarAsync(Input.Correo, ip, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al solicitar recuperación.");
            ErrorMessage = "El servicio no está disponible temporalmente. Intenta más tarde.";
            return Page();
        }

        SolicitudRecibida = true;
        Input = new SolicitudInput();
        ModelState.Clear();
        return Page();
    }

    private void CabecerasSeguridad()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    public sealed class SolicitudInput
    {
        [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(150, ErrorMessage = "El correo es demasiado largo.")]
        public string Correo { get; set; } = string.Empty;
    }
}
