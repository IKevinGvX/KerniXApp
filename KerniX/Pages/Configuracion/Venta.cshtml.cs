using System.ComponentModel.DataAnnotations;
using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion;

public sealed class VentaModel : PageModel
{
    private readonly TokenStorageService _tokenStorage;

    public VentaModel(
        TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    // ==========================================================
    // INPUT
    // ==========================================================

    [BindProperty]
    public ConfiguracionVentasInput Input { get; set; }
        = new();

    // ==========================================================
    // VISTA
    // ==========================================================

    public string Usuario { get; private set; }
        = "Usuario";

    public string? Mensaje { get; set; }

    public string? ErrorMessage { get; set; }


    // ==========================================================
    // GET
    // ==========================================================

    public IActionResult OnGet()
    {
        if (!_tokenStorage.EstaAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        Usuario =
            HttpContext.Session.GetString(
                SessionKeys.Usuario)
            ?? "Usuario";

        // ======================================================
        // POR AHORA CARGAMOS VALORES PREDETERMINADOS.
        //
        // Luego esto se reemplaza por:
        // GET configuración real desde API / BD.
        // ======================================================

        Input = ObtenerConfiguracionPredeterminada();

        return Page();
    }


    // ==========================================================
    // POST
    // ==========================================================

    public IActionResult OnPost()
    {
        if (!_tokenStorage.EstaAutenticado())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        Usuario =
            HttpContext.Session.GetString(
                SessionKeys.Usuario)
            ?? "Usuario";

        if (!ModelState.IsValid)
        {
            ErrorMessage =
                "Revisa los valores ingresados.";

            return Page();
        }

        // ======================================================
        // AQUÍ POSTERIORMENTE LLAMAMOS:
        //
        // ConfiguracionVentasService
        //          ↓
        // KerniX API
        //          ↓
        // cfg.*
        //
        // No inventamos el endpoint hasta revisar
        // tu API real.
        // ======================================================

        Mensaje =
            "La configuración de ventas fue validada correctamente.";

        return Page();
    }


    // ==========================================================
    // DEFAULT
    // ==========================================================

    private static ConfiguracionVentasInput
        ObtenerConfiguracionPredeterminada()
    {
        return new ConfiguracionVentasInput
        {
            Moneda = "PEN",

            PorcentajeIgv = 18,

            DecimalesPrecio = 2,

            ValidarStock = true,

            PermitirStockNegativo = false,

            ActualizarKardex = true,

            DescuentoMaximo = 10,

            RequiereAutorizacionDescuento = true,

            DiasValidezCotizacion = 15,

            ReservarStockPedido = true,

            PermitirVentaDirecta = true,

            TipoComprobantePredeterminado =
                "BOLETA",

            SerieBoleta = "B001",

            SerieFactura = "F001"
        };
    }


    // ==========================================================
    // DTO DE VISTA
    // ==========================================================

    public sealed class ConfiguracionVentasInput
    {
        [Required]
        public string Moneda { get; set; }
            = "PEN";


        [Range(0, 100)]
        public decimal PorcentajeIgv { get; set; }
            = 18;


        [Range(2, 4)]
        public int DecimalesPrecio { get; set; }
            = 2;


        public bool ValidarStock { get; set; }
            = true;


        public bool PermitirStockNegativo { get; set; }


        public bool ActualizarKardex { get; set; }
            = true;


        [Range(0, 100)]
        public decimal DescuentoMaximo { get; set; }
            = 10;


        public bool RequiereAutorizacionDescuento { get; set; }
            = true;


        [Range(1, 365)]
        public int DiasValidezCotizacion { get; set; }
            = 15;


        public bool ReservarStockPedido { get; set; }
            = true;


        public bool PermitirVentaDirecta { get; set; }
            = true;


        [Required]
        public string TipoComprobantePredeterminado { get; set; }
            = "BOLETA";


        [StringLength(10)]
        public string SerieBoleta { get; set; }
            = "B001";


        [StringLength(10)]
        public string SerieFactura { get; set; }
            = "F001";
    }
}