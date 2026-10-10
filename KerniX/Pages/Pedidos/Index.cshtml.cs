using KerniX.Models.Pedido;
using KerniX.Security;
using KerniX.Services.Pedido;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Pedidos;

public sealed class IndexModel : PageModel
{
    private readonly IPedidoService _pedidoService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPedidoService pedidoService,
        ILogger<IndexModel> logger)
    {
        _pedidoService = pedidoService;
        _logger = logger;
    }

    // ============================================
    // CONTEXTO DE SESIÓN
    // ============================================

    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }
    public int IdUsuario { get; private set; }
    public int IdEmpleado { get; private set; }

    // ============================================
    // DATOS
    // ============================================

    public IReadOnlyList<PedidoFila> Pedidos
    {
        get;
        private set;
    } = new List<PedidoFila>();

    // ============================================
    // FILTROS
    // ============================================

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    // ============================================
    // MENSAJES
    // ============================================

    public string? ErrorMensaje
    {
        get;
        private set;
    }

    // ============================================
    // INDICADORES
    // ============================================

    public int Total => Pedidos.Count;

    public int Pendientes =>
        Pedidos.Count(x =>
            string.Equals(
                x.Estado,
                "pendiente",
                StringComparison.OrdinalIgnoreCase
            )
        );

    public int Aprobados =>
        Pedidos.Count(x =>
            string.Equals(
                x.Estado,
                "aprobado",
                StringComparison.OrdinalIgnoreCase
            )
        );

    public int ConVenta =>
        Pedidos.Count(x => x.VentasVinculadas > 0);

    // ============================================
    // GET
    // ============================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        try
        {/* ====================================
   CARGAR CONTEXTO REAL DE SESIÓN
   ==================================== */

            IdUsuario =
                HttpContext.Session.GetInt32(
                    SessionKeys.IdUsuario
                ) ?? 0;

            IdEmpleado =
                HttpContext.Session.GetInt32(
                    SessionKeys.IdEmpleado
                ) ?? 0;

            // Estos dos valores todavía deben configurarse
            // correctamente durante el login.

            IdEmpresa =
                HttpContext.Session.GetInt32("IdEmpresa")
                ?? 1;

            IdSucursal =
                HttpContext.Session.GetInt32("IdSucursal")
                ?? 1;

            _logger.LogInformation(
                "[PEDIDOS] Contexto: Usuario={Usuario} " +
                "Empresa={Empresa} Sucursal={Sucursal} " +
                "Empleado={Empleado}",
                IdUsuario,
                IdEmpresa,
                IdSucursal,
                IdEmpleado
            );

            // ====================================
            // VALIDAR SESIÓN
            // ====================================

            if (IdUsuario <= 0 || IdEmpresa <= 0)
            {
                _logger.LogWarning(
                    "[PEDIDOS] Sesión incompleta. " +
                    "Usuario={Usuario}, Empresa={Empresa}",
                    IdUsuario,
                    IdEmpresa
                );

                return Redirect("/Account/Login");
            }

            // ====================================
            // VALIDAR SUCURSAL
            // ====================================

            if (IdSucursal <= 0)
            {
                ErrorMensaje =
                    "No se pudo identificar la sucursal. " +
                    "Selecciona una sucursal válida.";

                return Page();
            }

            // ====================================
            // NORMALIZAR BÚSQUEDA
            // ====================================

            Buscar =
                string.IsNullOrWhiteSpace(Buscar)
                    ? null
                    : Buscar.Trim();

            if (Buscar?.Length > 100)
            {
                Buscar = Buscar[..100];
            }

            // ====================================
            // NORMALIZAR ESTADO
            // ====================================

            Estado =
                string.IsNullOrWhiteSpace(Estado)
                    ? null
                    : Estado.Trim().ToLowerInvariant();

            // ====================================
            // VALIDACIÓN CORREGIDA
            // ====================================

            if (Estado is not null &&
                Estado != "pendiente" &&
                Estado != "aprobado" &&
                Estado != "parcial" &&
                Estado != "atendido" &&
                Estado != "cancelado")
            {
                ErrorMensaje =
                    "El estado seleccionado no es válido.";

                return Page();
            }

            // ====================================
            // FILTRO
            // ====================================

            var filtro = new PedidoFiltro
            {
                IdEmpresa = IdEmpresa,
                IdSucursal = IdSucursal,
                Buscar = Buscar,
                Estado = Estado
            };

            // ====================================
            // CONSULTAR PEDIDOS
            // ====================================

            Pedidos = await _pedidoService.ListarAsync(
                filtro,
                cancellationToken
            );

            _logger.LogInformation(
                "[PEDIDOS] Listado cargado. " +
                "Empresa={Empresa} Sucursal={Sucursal} " +
                "Registros={Registros}",
                IdEmpresa,
                IdSucursal,
                Pedidos.Count
            );

            return Page();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error SQL al listar pedidos."
            );

            ErrorMensaje =
                "No fue posible consultar las órdenes " +
                "de venta en SQL Server.";

            Pedidos = new List<PedidoFila>();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PEDIDOS] Error al cargar órdenes de venta."
            );

            ErrorMensaje =
                "Ocurrió un error al cargar los pedidos. " +
                "Revisa los registros del servidor.";

            Pedidos = new List<PedidoFila>();

            return Page();
        }
    }
}