using KerniX.Models.Clientes;
using KerniX.Security;
using KerniX.Services.Clientes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Cliente
{
    public class DetalleModel : PageModel
    {
        // ========================================
        // DEPENDENCIAS
        // ========================================

        private readonly IClienteService _clienteService;
        private readonly TokenStorageService _tokenStorage;
        private readonly ILogger<DetalleModel> _logger;

        public DetalleModel(
            IClienteService clienteService,
            TokenStorageService tokenStorage,
            ILogger<DetalleModel> logger)
        {
            _clienteService = clienteService;
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        // ========================================
        // CONFIGURACIÓN TEMPORAL
        // ========================================

        // Empresa fija únicamente para pruebas locales.
        // En producción debe obtenerse de la empresa
        // autorizada para el usuario autenticado.

        private const int EmpresaPruebas = 1;

        public int IdEmpresa { get; private set; }

        public int IdUsuario { get; private set; }

        // ========================================
        // PROPIEDADES
        // ========================================

        public ClienteModel? Cliente { get; private set; }

        public string? MensajeError { get; private set; }

        public bool TieneCliente => Cliente != null;

        public IndicadoresClienteModel Indicadores =>
            Cliente?.Indicadores
            ?? new IndicadoresClienteModel();

        public List<ContactoClienteModel> Contactos =>
            Cliente?.Contactos
            ?? new List<ContactoClienteModel>();

        public List<DireccionClienteModel> Direcciones =>
            Cliente?.Direcciones
            ?? new List<DireccionClienteModel>();

        // ========================================
        // VALIDAR SESIÓN
        // ========================================

        private bool SesionValida()
        {
            IdUsuario = HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario
            ) ?? 0;

            if (IdUsuario <= 0)
            {
                _logger.LogWarning(
                    "[DETALLE CLIENTE] No existe un IdUsuario válido."
                );

                return false;
            }

            if (!_tokenStorage.EstaAutenticado())
            {
                _logger.LogWarning(
                    "[DETALLE CLIENTE] No existe un JWT en la sesión."
                );

                return false;
            }

            IdEmpresa = EmpresaPruebas;

            return true;
        }

        // ========================================
        // GET - DETALLE DEL CLIENTE
        // URL: /Cliente/Detalle?idCliente=1
        // ========================================

        public async Task<IActionResult> OnGetAsync(
            int idCliente)
        {
            // 1. Validar autenticación.

            if (!SesionValida())
            {
                return RedirectToPage("/Account/Login");
            }

            _logger.LogInformation(
                "[DETALLE CLIENTE] Acceso. Usuario={IdUsuario}, Empresa={IdEmpresa}, Cliente={IdCliente}",
                IdUsuario,
                IdEmpresa,
                idCliente
            );

            // 2. Validar identificador.

            if (idCliente <= 0)
            {
                MensajeError =
                    "El identificador del cliente no es válido.";

                return Page();
            }

            try
            {
                // 3. Consultar cliente mediante la API.
                // ClienteService enviará el JWT automáticamente.

                var resultado =
                    await _clienteService.ObtenerAsync(idCliente);

                // 4. Verificar respuesta de la API.

                if (!resultado.Success)
                {
                    MensajeError =
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible consultar el cliente."
                            : resultado.Message;

                    _logger.LogWarning(
                        "[DETALLE CLIENTE] Error consultando cliente {IdCliente}: {Mensaje}",
                        idCliente,
                        MensajeError
                    );

                    return Page();
                }

                // 5. Verificar que el cliente existe.

                if (resultado.Data == null)
                {
                    MensajeError =
                        "No se encontró el cliente solicitado.";

                    _logger.LogWarning(
                        "[DETALLE CLIENTE] La API no devolvió datos para el cliente {IdCliente}.",
                        idCliente
                    );

                    return Page();
                }

                // 6. Validar empresa.
                // La API también debe comprobar que el usuario
                // está autorizado para acceder a esta empresa.

                if (resultado.Data.IdEmpresa != IdEmpresa)
                {
                    _logger.LogWarning(
                        "[DETALLE CLIENTE] Empresa no coincide. Cliente={IdCliente}, EmpresaCliente={EmpresaCliente}, EmpresaSesion={EmpresaSesion}",
                        idCliente,
                        resultado.Data.IdEmpresa,
                        IdEmpresa
                    );

                    MensajeError =
                        "El cliente solicitado no está disponible.";

                    return Page();
                }

                // 7. Asignar datos al modelo.

                Cliente = resultado.Data;

                _logger.LogInformation(
                    "[DETALLE CLIENTE] Cliente {IdCliente} cargado correctamente.",
                    Cliente.IdCliente
                );

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[DETALLE CLIENTE] Error inesperado al consultar el cliente {IdCliente}.",
                    idCliente
                );

                MensajeError =
                    "Ocurrió un error al cargar el detalle del cliente.";

                return Page();
            }
        }
    }
}