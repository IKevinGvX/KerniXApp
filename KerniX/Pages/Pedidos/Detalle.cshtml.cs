
using KerniX.Models.Pedido;
using KerniX.Security;
using KerniX.Services.Pedido;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Pedidos;

public sealed class DetalleModel : PageModel
{
    private readonly IPedidoService _pedidoService;

    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(
        IPedidoService pedidoService,
        ILogger<DetalleModel> logger)
    {
        _pedidoService = pedidoService;
        _logger = logger;
    }

    // ========================================
    // CONTEXTO DE PRUEBAS
    // ========================================

    private const int IdEmpresa = 1;

    private const int IdSucursal = 1;

    private int IdUsuario =>
        HttpContext.Session.GetInt32(
            SessionKeys.IdUsuario
        ) ?? 0;

    private int IdEmpleado =>
        HttpContext.Session.GetInt32(
            SessionKeys.IdEmpleado
        ) ?? 0;

    private bool Autorizado =>
        IdUsuario > 0 && IdEmpleado > 0;

    // ========================================
    // DATOS DEL PEDIDO
    // ========================================

    public PedidoDetalle Pedido { get; private set; }
        = null!;

    // ========================================
    // GET: CONSULTAR DETALLE
    // ========================================

    public async Task<IActionResult> OnGetAsync(
        long id,
        CancellationToken cancellationToken)
    {
        if (!Autorizado)
        {
            return Redirect("/Account/Login");
        }

        if (id <= 0)
        {
            return BadRequest(
                "El identificador del pedido no es válido."
            );
        }

        try
        {
            var pedido =
                await _pedidoService.ObtenerAsync(
                    id,
                    IdEmpresa,
                    IdSucursal,
                    cancellationToken
                );

            if (pedido is null)
            {
                return NotFound();
            }

            Pedido = pedido;

            _logger.LogInformation(
                "[PEDIDOS] Detalle cargado. Pedido={Pedido}, Usuario={Usuario}",
                id,
                IdUsuario
            );

            return Page();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error al obtener el pedido {Pedido}",
                id
            );

            return StatusCode(500);
        }
    }

    // ========================================
    // POST: APROBAR / CANCELAR
    // ========================================

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostCambiarEstadoAsync(
        long id,
        string? estado,
        CancellationToken cancellationToken)
    {
        // ====================================
        // SESIÓN
        // ====================================

        if (!Autorizado)
        {
            return Redirect("/Account/Login");
        }

        // ====================================
        // ID
        // ====================================

        if (id <= 0)
        {
            return BadRequest(
                "El identificador del pedido no es válido."
            );
        }

        // ====================================
        // ESTADO SOLICITADO
        // ====================================

        string nuevoEstado =
            (estado ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

        if (
            nuevoEstado != "aprobado" &&
            nuevoEstado != "cancelado"
        )
        {
            TempData["PedidoError"] =
                "La operación solicitada no es válida.";

            return RedirectToPage(
                "/Pedidos/Detalle",
                new { id }
            );
        }

        // ====================================
        // EJECUTAR CAMBIO
        // ====================================

        try
        {
            await _pedidoService.CambiarEstadoAsync(
                id,
                IdEmpresa,
                IdSucursal,
                nuevoEstado,
                cancellationToken
            );

            _logger.LogInformation(
                "[PEDIDOS] Estado modificado. Pedido={Pedido}, Estado={Estado}, Usuario={Usuario}, Empleado={Empleado}",
                id,
                nuevoEstado,
                IdUsuario,
                IdEmpleado
            );

            TempData["PedidoMensaje"] =
                nuevoEstado == "aprobado"
                    ? "El pedido fue aprobado correctamente."
                    : "El pedido fue cancelado correctamente.";
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            TempData["PedidoError"] =
                ex.Message;
        }
        catch (SqlException ex)
            when (ex.Number is >= 52200 and <= 52299)
        {
            _logger.LogWarning(
                ex,
                "[PEDIDOS] Validación SQL al cambiar estado. Pedido={Pedido}",
                id
            );

            TempData["PedidoError"] =
                ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error al cambiar estado. Pedido={Pedido}",
                id
            );

            TempData["PedidoError"] =
                "No se pudo confirmar el cambio de estado. " +
                "Actualiza la página y comprueba el estado antes de reintentarlo.";
        }

        // ====================================
        // RECARGAR ESTADO REAL DESDE SQL
        // ====================================

        return RedirectToPage(
            "/Pedidos/Detalle",
            new { id }
        );
    }
}