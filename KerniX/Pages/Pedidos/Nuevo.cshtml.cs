
using KerniX.Models.Pedido;
using KerniX.Security;
using KerniX.Services.Pedido;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Pedidos;

public sealed class NuevoModel : PageModel
{
    private readonly IPedidoService _service;
    private readonly ILogger<NuevoModel> _logger;

    public NuevoModel(
        IPedidoService service,
        ILogger<NuevoModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int Usuario =>
        HttpContext.Session.GetInt32(
            SessionKeys.IdUsuario
        ) ?? 0;

    private int Empleado =>
        HttpContext.Session.GetInt32(
            SessionKeys.IdEmpleado
        ) ?? 0;

    private bool Autorizado =>
        Usuario > 0 && Empleado > 0;

    // ============================================
    // CARGAR NUEVA ORDEN
    // ============================================

    public IActionResult OnGet()
    {
        if (!Autorizado)
        {
            return Redirect("/Account/Login");
        }

        return Page();
    }

    // ============================================
    // BUSCAR CLIENTES
    // ============================================

    public async Task<IActionResult> OnGetClientesAsync(
        string? buscar,
        CancellationToken cancellationToken)
    {
        if (!Autorizado)
        {
            return Unauthorized();
        }

        try
        {
            var clientes =
                await _service.BuscarClientesAsync(
                    buscar ?? string.Empty,
                    cancellationToken
                );

            return new JsonResult(clientes);
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
                "[PEDIDOS] Error al buscar clientes. Buscar={Buscar}",
                buscar
            );

            return StatusCode(500, new
            {
                mensaje = "No fue posible buscar clientes."
            });
        }
    }

    // ============================================
    // BUSCAR PRODUCTOS CON PRECIO DE VENTA
    // ============================================

    public async Task<IActionResult> OnGetProductosAsync(
        string? buscar,
        CancellationToken cancellationToken)
    {
        if (!Autorizado)
        {
            return Unauthorized();
        }

        _logger.LogInformation(
            "[PEDIDOS] Búsqueda de productos. Buscar={Buscar}",
            buscar
        );

        try
        {
            var productos =
                await _service.BuscarProductosAsync(
                    buscar ?? string.Empty,
                    cancellationToken
                );

            _logger.LogInformation(
                "[PEDIDOS] Productos encontrados={Cantidad}",
                productos.Count
            );

            // Cada producto debe incluir PrecioUnitario,
            // obtenido de inv.producto.precio_venta.

            return new JsonResult(productos);
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
                "[PEDIDOS] Error al buscar productos. Buscar={Buscar}",
                buscar
            );

            return StatusCode(500, new
            {
                mensaje = "No fue posible buscar productos."
            });
        }
    }

    // ============================================
    // BUSCAR COTIZACIONES
    // ============================================

    public async Task<IActionResult> OnGetCotizacionesAsync(
        int idCliente,
        CancellationToken cancellationToken)
    {
        if (!Autorizado)
        {
            return Unauthorized();
        }

        if (idCliente <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona un cliente válido."
            });
        }

        try
        {
            var cotizaciones =
                await _service.BuscarCotizacionesAsync(
                    idCliente,
                    cancellationToken
                );

            return new JsonResult(cotizaciones);
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
                "[PEDIDOS] Error al buscar cotizaciones. Cliente={Cliente}",
                idCliente
            );

            return StatusCode(500, new
            {
                mensaje = "No fue posible buscar cotizaciones."
            });
        }
    }

    // ============================================
    // REGISTRAR ORDEN
    // ============================================

    public async Task<IActionResult> OnPostRegistrarAsync(
        [FromBody] PedidoRegistrarRequest? request,
        CancellationToken cancellationToken)
    {
        // ========================================
        // 1. VALIDAR SESIÓN
        // ========================================

        if (!Autorizado)
        {
            return StatusCode(401, new
            {
                correcto = false,
                sesionExpirada = true,
                mensaje = "La sesión ha expirado."
            });
        }

        // ========================================
        // 2. VALIDAR REQUEST
        // ========================================

        if (request is null)
        {
            _logger.LogWarning(
                "[PEDIDOS] Registro rechazado: request nulo."
            );

            return BadRequest(new
            {
                correcto = false,
                mensaje = "No se recibieron los datos de la orden."
            });
        }

        // ========================================
        // 3. VALIDAR MODELSTATE
        // ========================================

        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x =>
                    x.Value!.Errors.Select(error => new
                    {
                        campo = x.Key,
                        error = string.IsNullOrWhiteSpace(
                            error.ErrorMessage
                        )
                            ? "El valor enviado no es válido."
                            : error.ErrorMessage
                    })
                )
                .ToList();

            foreach (var error in errores)
            {
                _logger.LogWarning(
                    "[PEDIDOS] Campo inválido={Campo}. Error={Error}",
                    error.campo,
                    error.error
                );
            }

            return BadRequest(new
            {
                correcto = false,
                mensaje = "Hay campos inválidos en la orden.",
                errores
            });
        }

        // ========================================
        // 4. VALIDAR CLIENTE
        // ========================================

        if (request.IdCliente <= 0)
        {
            return BadRequest(new
            {
                correcto = false,
                mensaje = "Selecciona un cliente válido."
            });
        }

        // ========================================
        // 5. VALIDAR DETALLES
        // ========================================

        if (request.Detalles is null ||
            request.Detalles.Count == 0)
        {
            return BadRequest(new
            {
                correcto = false,
                mensaje = "Agrega al menos un producto."
            });
        }

        // ========================================
        // 6. REGISTRAR EN SQL SERVER
        // ========================================

        try
        {
            long idPedido =
                await _service.RegistrarAsync(
                    request,
                    Empleado,
                    cancellationToken
                );

            _logger.LogInformation(
                "[PEDIDOS] Orden registrada. IdPedido={IdPedido}, Usuario={Usuario}, Empleado={Empleado}",
                idPedido,
                Usuario,
                Empleado
            );

            return new JsonResult(new
            {
                correcto = true,
                idPedido,
                mensaje = "Orden registrada correctamente."
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "[PEDIDOS] Registro rechazado: {Mensaje}",
                ex.Message
            );

            return BadRequest(new
            {
                correcto = false,
                mensaje = ex.Message
            });
        }
        catch (OverflowException ex)
        {
            _logger.LogWarning(
                ex,
                "[PEDIDOS] Los importes superan el límite permitido."
            );

            return BadRequest(new
            {
                correcto = false,
                mensaje = "Los importes superan el límite permitido."
            });
        }
        catch (SqlException ex)
            when (ex.Number is >= 52200 and <= 52299)
        {
            _logger.LogWarning(
                ex,
                "[PEDIDOS] Validación SQL. Código={Codigo}",
                ex.Number
            );

            return BadRequest(new
            {
                correcto = false,
                mensaje = ex.Message
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error SQL al registrar. Comprobar estado antes de reintentar."
            );

            return StatusCode(500, new
            {
                correcto = false,
                mensaje = "No se confirmó el registro. Revisa el directorio antes de reintentarlo."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error inesperado al registrar."
            );

            return StatusCode(500, new
            {
                correcto = false,
                mensaje = "No se confirmó el registro. Verifica el directorio antes de volver a intentarlo."
            });
        }
    }
}