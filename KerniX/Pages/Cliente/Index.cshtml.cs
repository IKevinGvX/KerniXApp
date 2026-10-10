using KerniX.Models.Clientes;
using KerniX.Security;
using KerniX.Services.Clientes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KerniX.Pages.Cliente
{
    public class IndexModel : PageModel
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IClienteService clienteService,
            ILogger<IndexModel> logger)
        {
            _clienteService = clienteService;
            _logger = logger;
        }

        // ==========================================
        // CONFIGURACIÓN
        // ==========================================

        public int IdEmpresa { get; private set; }

        // TEMPORAL: EMPRESA 1 PARA PRUEBAS LOCALES.
        // La API debe comprobar que el usuario tiene
        // autorización para acceder a esta empresa.

        private int ObtenerIdEmpresa()
        {
            return 1;
        }

        private int ObtenerIdUsuario()
        {
            return HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario
            ) ?? 0;
        }

        private bool SesionValida()
        {
            return ObtenerIdUsuario() > 0;
        }

        // ==========================================
        // RESPUESTAS DE ERROR
        // ==========================================

        private IActionResult ErrorSesion()
        {
            return new JsonResult(new
            {
                success = false,
                message = "La sesión ha expirado. Inicia sesión nuevamente."
            })
            {
                StatusCode = 401
            };
        }

        private IActionResult ErrorValidacion(string mensaje)
        {
            return new JsonResult(new
            {
                success = false,
                message = mensaje
            })
            {
                StatusCode = 400
            };
        }

        private IActionResult ErrorNoEncontrado()
        {
            return new JsonResult(new
            {
                success = false,
                message = "El cliente solicitado no existe."
            })
            {
                StatusCode = 404
            };
        }

        private IActionResult ErrorInterno()
        {
            return new JsonResult(new
            {
                success = false,
                message = "Ocurrió un error inesperado. Inténtalo nuevamente."
            })
            {
                StatusCode = 500
            };
        }

        // ==========================================
        // GET - PÁGINA PRINCIPAL
        // ==========================================

        public IActionResult OnGet()
        {
            var idUsuario = ObtenerIdUsuario();

            if (idUsuario <= 0)
            {
                _logger.LogWarning(
                    "[CLIENTES] Usuario sin sesión. Redirigiendo al Login."
                );

                return RedirectToPage("/Account/Login");
            }

            IdEmpresa = ObtenerIdEmpresa();

            _logger.LogInformation(
                "[CLIENTES] Acceso. Usuario={Usuario}, Empresa={Empresa}",
                idUsuario,
                IdEmpresa
            );

            return Page();
        }

        // ==========================================
        // GET - LISTAR CLIENTES
        // URL: ?handler=Listar
        // ==========================================

        public async Task<IActionResult> OnGetListarAsync()
        {
            if (!SesionValida())
            {
                return ErrorSesion();
            }

            try
            {
                var resultado =
                    await _clienteService.ListarAsync();

                if (!resultado.Success)
                {
                    _logger.LogWarning(
                        "No se pudieron listar clientes: {Mensaje}",
                        resultado.Message
                    );

                    return new JsonResult(resultado)
                    {
                        StatusCode = 502
                    };
                }

                var idEmpresa = ObtenerIdEmpresa();

                var clientes = resultado.Data?.Items
                    ?? new List<ClienteModel>();

                var clientesEmpresa = clientes
                    .Where(x => x.IdEmpresa == idEmpresa)
                    .ToList();

                return new JsonResult(new
                {
                    success = true,
                    message = resultado.Message,
                    data = new
                    {
                        items = clientesEmpresa,
                        total = clientesEmpresa.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al listar clientes."
                );

                return ErrorInterno();
            }
        }

        // ==========================================
        // GET - OBTENER CLIENTE
        // URL: ?handler=Obtener&idCliente=2
        // ==========================================

        public async Task<IActionResult> OnGetObtenerAsync(
            int idCliente)
        {
            if (!SesionValida())
            {
                return ErrorSesion();
            }

            if (idCliente <= 0)
            {
                return ErrorValidacion(
                    "El identificador del cliente no es válido."
                );
            }

            try
            {
                var resultado =
                    await _clienteService.ObtenerAsync(idCliente);

                if (!resultado.Success)
                {
                    return new JsonResult(resultado)
                    {
                        StatusCode = 502
                    };
                }

                if (resultado.Data == null)
                {
                    return ErrorNoEncontrado();
                }

                if (resultado.Data.IdEmpresa != ObtenerIdEmpresa())
                {
                    return ErrorNoEncontrado();
                }

                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar cliente {IdCliente}",
                    idCliente
                );

                return ErrorInterno();
            }
        }

        // ==========================================
        // POST - REGISTRAR CLIENTE
        // URL: ?handler=Registrar
        // ==========================================

        public async Task<IActionResult> OnPostRegistrarAsync()
        {
            if (!SesionValida())
            {
                return ErrorSesion();
            }

            try
            {
                var request = await LeerRequestAsync();

                if (request == null)
                {
                    return ErrorValidacion(
                        "No se recibieron datos válidos del cliente."
                    );
                }

                // Nunca confiar en el IdEmpresa enviado
                // por el navegador.

                request.IdEmpresa = ObtenerIdEmpresa();

                NormalizarCliente(request);

                var validacion = ValidarCliente(request);

                if (validacion != null)
                {
                    return ErrorValidacion(validacion);
                }

                var resultado =
                    await _clienteService.RegistrarAsync(request);

                if (!resultado.Success)
                {
                    return new JsonResult(resultado)
                    {
                        StatusCode = 400
                    };
                }

                _logger.LogInformation(
                    "Cliente registrado. Empresa={IdEmpresa}, Cliente={IdCliente}",
                    request.IdEmpresa,
                    resultado.Data?.IdCliente
                );

                return new JsonResult(resultado)
                {
                    StatusCode = 201
                };
            }
            catch (JsonException)
            {
                return ErrorValidacion(
                    "El JSON enviado no tiene un formato válido."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al registrar cliente."
                );

                return ErrorInterno();
            }
        }

        // ==========================================
        // POST - ACTUALIZAR CLIENTE
        // URL: ?handler=Actualizar&idCliente=2
        // ==========================================

        public async Task<IActionResult> OnPostActualizarAsync(
            int idCliente)
        {
            if (!SesionValida())
            {
                return ErrorSesion();
            }

            if (idCliente <= 0)
            {
                return ErrorValidacion(
                    "El identificador del cliente no es válido."
                );
            }

            try
            {
                var existente =
                    await _clienteService.ObtenerAsync(idCliente);

                if (!existente.Success || existente.Data == null)
                {
                    return ErrorNoEncontrado();
                }

                if (existente.Data.IdEmpresa != ObtenerIdEmpresa())
                {
                    return ErrorNoEncontrado();
                }

                var request = await LeerRequestAsync();

                if (request == null)
                {
                    return ErrorValidacion(
                        "No se recibieron datos válidos."
                    );
                }

                request.IdEmpresa = ObtenerIdEmpresa();

                // El estado se modifica únicamente
                // mediante Activar y Desactivar.

                request.Estado = existente.Data.Estado;

                NormalizarCliente(request);

                var validacion = ValidarCliente(request);

                if (validacion != null)
                {
                    return ErrorValidacion(validacion);
                }

                var resultado =
                    await _clienteService.ActualizarAsync(
                        idCliente,
                        request
                    );

                if (!resultado.Success)
                {
                    return new JsonResult(resultado)
                    {
                        StatusCode = 400
                    };
                }

                _logger.LogInformation(
                    "Cliente actualizado: {IdCliente}",
                    idCliente
                );

                return new JsonResult(resultado);
            }
            catch (JsonException)
            {
                return ErrorValidacion(
                    "El JSON enviado no es válido."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar cliente {IdCliente}",
                    idCliente
                );

                return ErrorInterno();
            }
        }

        // ==========================================
        // POST - ACTIVAR CLIENTE
        // ==========================================

        public async Task<IActionResult> OnPostActivarAsync(
            int idCliente)
        {
            return await CambiarEstadoAsync(
                idCliente,
                true
            );
        }

        // ==========================================
        // POST - DESACTIVAR CLIENTE
        // ==========================================

        public async Task<IActionResult> OnPostDesactivarAsync(
            int idCliente)
        {
            return await CambiarEstadoAsync(
                idCliente,
                false
            );
        }

        // ==========================================
        // CAMBIAR ESTADO
        // ==========================================

        private async Task<IActionResult> CambiarEstadoAsync(
            int idCliente,
            bool activar)
        {
            if (!SesionValida())
            {
                return ErrorSesion();
            }

            if (idCliente <= 0)
            {
                return ErrorValidacion(
                    "El identificador del cliente no es válido."
                );
            }

            try
            {
                var existente =
                    await _clienteService.ObtenerAsync(idCliente);

                if (!existente.Success || existente.Data == null)
                {
                    return ErrorNoEncontrado();
                }

                if (existente.Data.IdEmpresa != ObtenerIdEmpresa())
                {
                    return ErrorNoEncontrado();
                }

                if (existente.Data.Estado == activar)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = activar
                            ? "El cliente ya se encuentra activo."
                            : "El cliente ya se encuentra inactivo."
                    });
                }

                var resultado = activar
                    ? await _clienteService.ActivarAsync(idCliente)
                    : await _clienteService.DesactivarAsync(idCliente);

                if (!resultado.Success)
                {
                    return new JsonResult(resultado)
                    {
                        StatusCode = 400
                    };
                }

                _logger.LogInformation(
                    "Estado de cliente modificado. Cliente={IdCliente}, Activo={Estado}",
                    idCliente,
                    activar
                );

                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado de cliente {IdCliente}",
                    idCliente
                );

                return ErrorInterno();
            }
        }

        // ==========================================
        // LEER JSON
        // ==========================================

        private async Task<ClienteRequest?> LeerRequestAsync()
        {
            return await Request.ReadFromJsonAsync<ClienteRequest>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }

        // ==========================================
        // NORMALIZAR CLIENTE
        // ==========================================

        private static void NormalizarCliente(
            ClienteRequest cliente)
        {
            cliente.TipoCliente =
                (cliente.TipoCliente ?? "")
                    .Trim()
                    .ToLowerInvariant();

            cliente.TipoDocumento =
                (cliente.TipoDocumento ?? "")
                    .Trim()
                    .ToUpperInvariant();

            cliente.NumeroDocumento =
                (cliente.NumeroDocumento ?? "")
                    .Trim();

            cliente.RazonSocial =
                cliente.RazonSocial?.Trim();

            cliente.Nombres =
                cliente.Nombres?.Trim();

            cliente.Apellidos =
                cliente.Apellidos?.Trim();

            cliente.Telefono =
                cliente.Telefono?.Trim();

            cliente.Correo =
                cliente.Correo?.Trim();

            cliente.Contactos ??= new();
            cliente.Direcciones ??= new();

            foreach (var contacto in cliente.Contactos)
            {
                contacto.Nombres =
                    contacto.Nombres?.Trim() ?? "";

                contacto.Cargo =
                    contacto.Cargo?.Trim() ?? "";

                contacto.Telefono =
                    contacto.Telefono?.Trim() ?? "";

                contacto.Correo =
                    contacto.Correo?.Trim() ?? "";
            }

            foreach (var direccion in cliente.Direcciones)
            {
                direccion.TipoDireccion =
                    direccion.TipoDireccion?.Trim() ?? "";

                direccion.Direccion =
                    direccion.Direccion?.Trim() ?? "";

                direccion.Referencia =
                    direccion.Referencia?.Trim() ?? "";

                direccion.Distrito =
                    direccion.Distrito?.Trim() ?? "";

                direccion.Provincia =
                    direccion.Provincia?.Trim() ?? "";

                direccion.Departamento =
                    direccion.Departamento?.Trim() ?? "";

                direccion.Ubigeo =
                    direccion.Ubigeo?.Trim() ?? "";
            }
        }

        // ==========================================
        // VALIDAR CLIENTE
        // ==========================================

        private static string? ValidarCliente(
            ClienteRequest cliente)
        {
            var errores = new List<ValidationResult>();

            var contexto =
                new ValidationContext(cliente);

            var valido =
                Validator.TryValidateObject(
                    cliente,
                    contexto,
                    errores,
                    validateAllProperties: true
                );

            if (!valido)
            {
                return errores.FirstOrDefault()?.ErrorMessage
                    ?? "Los datos del cliente no son válidos.";
            }

            if (cliente.TipoCliente != "natural" &&
                cliente.TipoCliente != "juridico")
            {
                return "El tipo de cliente debe ser natural o juridico.";
            }

            if (cliente.TipoDocumento == "DNI")
            {
                if (cliente.NumeroDocumento.Length != 8 ||
                    !cliente.NumeroDocumento.All(char.IsDigit))
                {
                    return "El DNI debe contener exactamente 8 dígitos.";
                }
            }

            if (cliente.TipoDocumento == "RUC")
            {
                if (cliente.NumeroDocumento.Length != 11 ||
                    !cliente.NumeroDocumento.All(char.IsDigit))
                {
                    return "El RUC debe contener exactamente 11 dígitos.";
                }
            }

            if (cliente.TipoCliente == "juridico")
            {
                if (string.IsNullOrWhiteSpace(cliente.RazonSocial))
                {
                    return "La razón social es obligatoria.";
                }

                if (cliente.TipoDocumento != "RUC")
                {
                    return "Una persona jurídica debe registrar RUC.";
                }
            }

            if (cliente.TipoCliente == "natural")
            {
                if (string.IsNullOrWhiteSpace(cliente.Nombres))
                {
                    return "Los nombres son obligatorios.";
                }

                if (string.IsNullOrWhiteSpace(cliente.Apellidos))
                {
                    return "Los apellidos son obligatorios.";
                }
            }

            if (cliente.Contactos.Count(x => x.EsPrincipal) > 1)
            {
                return "Solo puede existir un contacto principal.";
            }

            if (cliente.Direcciones.Count(x => x.EsPrincipal) > 1)
            {
                return "Solo puede existir una dirección principal.";
            }

            return null;
        }
    }
}