using KerniX.Models.Cotizacion;
using KerniX.Security;
using KerniX.Services.Cotizacion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace KerniX.Pages.Cotizacion;

public sealed class DetalleModel : PageModel
{
    private readonly ICotizacionDbService _cotizacionDbService;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(
        ICotizacionDbService cotizacionDbService,
        ILogger<DetalleModel> logger)
    {
        _cotizacionDbService = cotizacionDbService;
        _logger = logger;
    }
    public VentaVinculadaCotizacion? VentaVinculada
    {
        get;
        private set;
    }
    public CotizacionDetalleResponse Cotizacion { get; private set; }
        = new();

    public List<AlmacenCotizacionItem> Almacenes
    {
        get;
        private set;
    } = new();


    // =========================================================
    // GET - DETALLE
    // =========================================================

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0)
        {
            return BadRequest(
                "El identificador de la cotización no es válido."
            );
        }

        // =====================================================
        // USUARIO AUTENTICADO
        // =====================================================

        int idUsuario =
            HttpContext.Session.GetInt32(SessionKeys.IdUsuario)
            ?? 0;

        if (idUsuario <= 0)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // =================================================
            // EMPRESA Y SUCURSAL
            // =================================================

            int idEmpresa =
                HttpContext.Session.GetInt32("IdEmpresa")
                ?? 0;

            int idSucursal =
                HttpContext.Session.GetInt32("IdSucursal")
                ?? 0;

            // Si todavía no existen en sesión,
            // recuperarlos del JWT almacenado por la aplicación.

            if (idEmpresa <= 0 || idSucursal <= 0)
            {
                string? token =
                    HttpContext.Session.GetString(
                        SessionKeys.AccessToken
                    );

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning(
                        "No existe token de acceso en sesión. Usuario={Usuario}",
                        idUsuario
                    );

                    return RedirectToPage("/Account/Login");
                }

                var contexto = ObtenerContextoDesdeToken(token);

                // Comprobar que el token corresponde
                // al usuario autenticado en esta sesión.

                if (contexto.IdUsuario != idUsuario)
                {
                    _logger.LogWarning(
                        "El usuario del token no coincide con la sesión."
                    );

                    return RedirectToPage("/Account/Login");
                }

                idEmpresa = contexto.IdEmpresa;
                idSucursal = contexto.IdSucursal;

                // Guardar los valores obtenidos para
                // las siguientes consultas.

                HttpContext.Session.SetInt32(
                    "IdEmpresa",
                    idEmpresa
                );

                HttpContext.Session.SetInt32(
                    "IdSucursal",
                    idSucursal
                );
            }

            // =================================================
            // VALIDAR CONTEXTO
            // =================================================

            if (idEmpresa <= 0 || idSucursal <= 0)
            {
                _logger.LogWarning(
                    "Contexto empresarial inválido. Usuario={Usuario}",
                    idUsuario
                );

                return StatusCode(
                    403,
                    "No se pudo identificar la empresa y sucursal."
                );
            }

            _logger.LogInformation(
                "Consultando cotización. Id={Id} | Empresa={Empresa} | Sucursal={Sucursal}",
                id,
                idEmpresa,
                idSucursal
            );

            // =================================================
            // CONSULTAR COTIZACIÓN
            // =================================================

            CotizacionDetalleResponse? resultado =
                await _cotizacionDbService.ObtenerPorIdAsync(
                    id,
                    idEmpresa,
                    idSucursal
                );

            if (resultado is null)
            {
                return NotFound(
                    "La cotización no existe o no pertenece a la empresa y sucursal."
                );
            }

            Cotizacion = resultado;

            // Venta vinculada: consultar en cualquier estado (especialmente Convertida).
            VentaVinculada = await _cotizacionDbService.ObtenerVentaVinculadaAsync(
                id, idEmpresa, idSucursal
            );

            // Mostrar selector únicamente si aún es posible convertirla.
            if (string.Equals(
                Cotizacion.Estado,
                "Aprobada",
                StringComparison.OrdinalIgnoreCase))
            {
                Almacenes = await _cotizacionDbService.ListarAlmacenesAsync(
                    idEmpresa, idSucursal
                );
            }

            _logger.LogInformation(
                "Cotización consultada. Id={Id} | Número={Numero} | Productos={Productos}",
                Cotizacion.IdCotizacion,
                Cotizacion.NumeroCotizacion,
                Cotizacion.Productos.Count
            );

            return Page();
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                ex,
                "Token de sesión con formato inválido."
            );

            return RedirectToPage("/Account/Login");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo leer el contexto del token."
            );

            return RedirectToPage("/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consultando cotización. Id={Id}",
                id
            );

            return StatusCode(
                500,
                "No se pudo consultar el detalle de la cotización."
            );
        }
    }

    // =========================================================
    // OBTENER CONTEXTO DEL JWT
    // =========================================================

    private static (
        int IdUsuario,
        int IdEmpresa,
        int IdSucursal
    ) ObtenerContextoDesdeToken(string token)
    {
        string[] partes = token.Split('.');

        if (partes.Length != 3)
        {
            throw new FormatException(
                "El token JWT no tiene un formato válido."
            );
        }

        string payload = partes[1]
            .Replace('-', '+')
            .Replace('_', '/');

        payload = payload.PadRight(
            ((payload.Length + 3) / 4) * 4,
            '='
        );

        byte[] bytes =
            Convert.FromBase64String(payload);

        string json =
            Encoding.UTF8.GetString(bytes);

        using JsonDocument documento =
            JsonDocument.Parse(json);

        JsonElement claims =
            documento.RootElement;

        int idUsuario =
            ObtenerClaimEntero(claims, "sub");

        int idEmpresa =
            ObtenerClaimEntero(claims, "id_empresa");

        int idSucursal =
            ObtenerClaimEntero(claims, "id_sucursal");

        if (idUsuario <= 0 ||
            idEmpresa <= 0 ||
            idSucursal <= 0)
        {
            throw new FormatException(
                "El token no contiene un contexto empresarial válido."
            );
        }

        return (
            idUsuario,
            idEmpresa,
            idSucursal
        );
    }

    // =========================================================
    // LEER CLAIM ENTERO
    // =========================================================

    private static int ObtenerClaimEntero(
        JsonElement claims,
        string nombre)
    {
        if (!claims.TryGetProperty(
            nombre,
            out JsonElement propiedad))
        {
            return 0;
        }

        if (propiedad.ValueKind == JsonValueKind.Number &&
            propiedad.TryGetInt32(out int numero))
        {
            return numero;
        }

        if (propiedad.ValueKind == JsonValueKind.String &&
            int.TryParse(
                propiedad.GetString(),
                out int valor))
        {
            return valor;
        }

        return 0;
    }
    // =========================================================
    // POST - APROBAR
    // =========================================================

    public async Task<IActionResult> OnPostAprobarAsync(
        long id)
    {
        return await CambiarEstadoAsync(
            id,
            "Aprobar"
        );
    }


    // =========================================================
    // POST - RECHAZAR
    // =========================================================

    public async Task<IActionResult> OnPostRechazarAsync(
        long id)
    {
        return await CambiarEstadoAsync(
            id,
            "Rechazar"
        );
    }


    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================
    private async Task<IActionResult> CambiarEstadoAsync(
        long id,
        string accion)
    {
        if (id <= 0)
        {
            return BadRequest(
                "La cotización no es válida."
            );
        }

        // =====================================================
        // VALIDAR USUARIO
        // =====================================================

        int idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario
            ) ?? 0;

        if (idUsuario <= 0)
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        // =====================================================
        // OBTENER CONTEXTO EMPRESARIAL
        // =====================================================

        int idEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa"
            ) ?? 0;

        int idSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal"
            ) ?? 0;

        if (idEmpresa <= 0 ||
            idSucursal <= 0)
        {
            return StatusCode(
                403,
                "No se pudo identificar la empresa o sucursal."
            );
        }

        try
        {
            // =================================================
            // COMPROBAR QUE LA COTIZACIÓN ES ACCESIBLE
            // =================================================

            CotizacionDetalleResponse? cotizacion =
                await _cotizacionDbService.ObtenerPorIdAsync(
                    id,
                    idEmpresa,
                    idSucursal
                );

            if (cotizacion is null)
            {
                return NotFound(
                    "No se encontró la cotización."
                );
            }

            // =================================================
            // VALIDAR ESTADO
            // =================================================

            if (!string.Equals(
                cotizacion.Estado,
                "Pendiente",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["CotizacionError"] =
                    "Solo se pueden aprobar o rechazar cotizaciones pendientes.";

                return RedirectToPage(
                    "./Detalle",
                    new { id }
                );
            }

            // =================================================
            // EJECUTAR PROCEDIMIENTO
            // =================================================

            if (accion == "Aprobar")
            {
                await _cotizacionDbService.AprobarAsync(
                    id,
                    idEmpresa,
                    idSucursal
                );

                TempData["CotizacionExito"] =
                    "La cotización fue aprobada correctamente.";
            }
            else if (accion == "Rechazar")
            {
                await _cotizacionDbService.RechazarAsync(
                    id,
                    idEmpresa,
                    idSucursal
                );

                TempData["CotizacionExito"] =
                    "La cotización fue rechazada correctamente.";
            }
            else
            {
                return BadRequest(
                    "La acción solicitada no es válida."
                );
            }

            _logger.LogInformation(
                "Cambio de estado de cotización. Id={Id} | Acción={Accion} | Usuario={Usuario}",
                id,
                accion,
                idUsuario
            );

            // =================================================
            // REDIRECCIÓN AL DETALLE
            // =================================================

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }
        catch (SqlException ex)
            when (ex.Number == 51002 ||
                  ex.Number == 51012)
        {
            _logger.LogWarning(
                ex,
                "No se pudo cambiar el estado de la cotización {Id}",
                id
            );

            TempData["CotizacionError"] =
                "La cotización ya no está pendiente o no pertenece al contexto indicado.";

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando el estado de la cotización {Id}",
                id
            );

            TempData["CotizacionError"] =
                "No se pudo actualizar el estado de la cotización.";

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }
    }
    // =========================================================
    // POST - CONVERTIR COTIZACIÓN EN VENTA
    // =========================================================

    public async Task<IActionResult> OnPostConvertirAsync(
        long id,
        int idAlmacen)
    {
        if (id <= 0 || idAlmacen <= 0)
        {
            TempData["CotizacionError"] =
                "Debes seleccionar un almacén válido.";

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }

        // =====================================================
        // VALIDAR USUARIO
        // =====================================================

        int idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario
            ) ?? 0;

        if (idUsuario <= 0)
        {
            return RedirectToPage("/Account/Login");
        }

        // =====================================================
        // CONTEXTO EMPRESARIAL DEL SERVIDOR
        // =====================================================

        int idEmpresa =
            HttpContext.Session.GetInt32("IdEmpresa")
            ?? 0;

        int idSucursal =
            HttpContext.Session.GetInt32("IdSucursal")
            ?? 0;

        if (idEmpresa <= 0 || idSucursal <= 0)
        {
            return StatusCode(
                403,
                "No existe un contexto empresarial válido."
            );
        }

        try
        {
            // =================================================
            // VALIDAR QUE LA COTIZACIÓN ES ACCESIBLE
            // =================================================

            CotizacionDetalleResponse? cotizacion =
                await _cotizacionDbService.ObtenerPorIdAsync(
                    id,
                    idEmpresa,
                    idSucursal
                );

            if (cotizacion is null)
            {
                return NotFound(
                    "La cotización no existe en la empresa y sucursal."
                );
            }

            // =================================================
            // VALIDAR ESTADO
            // =================================================

            if (!string.Equals(
                cotizacion.Estado,
                "Aprobada",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["CotizacionError"] =
                    "Solo se pueden convertir cotizaciones aprobadas.";

                return RedirectToPage(
                    "./Detalle",
                    new { id }
                );
            }

            // =================================================
            // VALIDAR ALMACÉN
            // =================================================

            List<AlmacenCotizacionItem> almacenes =
                await _cotizacionDbService.ListarAlmacenesAsync(
                    idEmpresa,
                    idSucursal
                );

            if (!almacenes.Any(
                a => a.IdAlmacen == idAlmacen))
            {
                TempData["CotizacionError"] =
                    "El almacén seleccionado no pertenece a la sucursal o está inactivo.";

                return RedirectToPage(
                    "./Detalle",
                    new { id }
                );
            }

            // =================================================
            // EJECUTAR CONVERSIÓN
            // =================================================

            long idVenta =
                await _cotizacionDbService.ConvertirVentaAsync(
                    id,
                    idEmpresa,
                    idSucursal,
                    idAlmacen
                );

            _logger.LogInformation(
                "Cotización convertida. Cotizacion={IdCotizacion} | Venta={IdVenta} | Usuario={IdUsuario}",
                id,
                idVenta,
                idUsuario
            );

            TempData["CotizacionExito"] =
                $"Cotización convertida correctamente. Venta registrada: {idVenta}.";

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }

        catch (SqlException ex)
            when (ex.Number >= 51200 &&
                  ex.Number <= 51213)
        {
            _logger.LogWarning(
                ex,
                "Conversión rechazada por SQL. Cotización={Id}",
                id
            );

            TempData["CotizacionError"] =
                ex.Message;

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al convertir cotización {Id}",
                id
            );

            TempData["CotizacionError"] =
                "No se pudo completar la conversión. Comprueba el estado de la cotización antes de volver a intentarlo.";

            return RedirectToPage(
                "./Detalle",
                new { id }
            );
        }
    }
}