using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KerniX.Models.Clientes;
using KerniX.Security;

namespace KerniX.Services.Clientes
{
    public class ClienteService : IClienteService
    {
        // ==========================================
        // DEPENDENCIAS
        // ==========================================

        private readonly HttpClient _http;
        private readonly TokenStorageService _tokenStorage;

        private readonly JsonSerializerOptions _jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public ClienteService(
            HttpClient http,
            TokenStorageService tokenStorage)
        {
            _http = http;
            _tokenStorage = tokenStorage;
        }

        // ==========================================
        // OBTENER JWT DE LA SESIÓN ACTUAL
        // ==========================================

        private string ObtenerToken()
        {
            var token = _tokenStorage.ObtenerToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "No se encontró el token de acceso. " +
                    "Inicia sesión nuevamente."
                );
            }

            return token;
        }

        // ==========================================
        // EJECUTAR REQUEST
        // ==========================================

        private async Task<ApiResponse<T>> EjecutarAsync<T>(
            HttpMethod metodo,
            string ruta,
            object? contenido = null)
        {
            try
            {
                // Obtener el JWT que guardó el Login.
                var token = ObtenerToken();

                // Crear una solicitud independiente.
                using var request =
                    new HttpRequestMessage(
                        metodo,
                        ruta
                    );

                // Agregar JWT únicamente a esta solicitud.
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token
                    );

                // Agregar cuerpo JSON para POST y PUT.
                if (contenido != null)
                {
                    request.Content =
                        JsonContent.Create(
                            contenido,
                            options: _jsonOptions
                        );
                }

                // Ejecutar solicitud.
                using var response =
                    await _http.SendAsync(request);

                // Leer respuesta.
                var texto =
                    await response.Content.ReadAsStringAsync();

                ApiResponse<T>? resultado = null;

                // ======================================
                // DESERIALIZAR JSON
                // ======================================

                if (!string.IsNullOrWhiteSpace(texto))
                {
                    try
                    {
                        resultado =
                            JsonSerializer.Deserialize<ApiResponse<T>>(
                                texto,
                                _jsonOptions
                            );
                    }
                    catch (JsonException)
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message =
                                "La API devolvió una respuesta JSON inválida."
                        };
                    }
                }

                // ======================================
                // RESPUESTAS HTTP NO EXITOSAS
                // ======================================

                if (!response.IsSuccessStatusCode)
                {
                    var mensaje = resultado?.Message;

                    if (string.IsNullOrWhiteSpace(mensaje))
                    {
                        mensaje =
                            response.StatusCode switch
                            {
                                System.Net.HttpStatusCode.Unauthorized =>
                                    "El token no es válido o ha expirado. " +
                                    "Inicia sesión nuevamente.",

                                System.Net.HttpStatusCode.Forbidden =>
                                    "Tu usuario no tiene permisos " +
                                    "para realizar esta operación.",

                                System.Net.HttpStatusCode.NotFound =>
                                    "El recurso solicitado no existe.",

                                System.Net.HttpStatusCode.BadRequest =>
                                    "La API rechazó los datos enviados.",

                                _ =>
                                    $"Error HTTP {(int)response.StatusCode}."
                            };
                    }

                    return new ApiResponse<T>
                    {
                        Success = false,
                        Message = mensaje,
                        Errors = resultado?.Errors
                    };
                }

                // ======================================
                // RESPUESTA EXITOSA
                // ======================================

                if (resultado != null)
                {
                    return resultado;
                }

                return new ApiResponse<T>
                {
                    Success = false,
                    Message =
                        "La API devolvió una respuesta vacía."
                };
            }

            // ==========================================
            // ERRORES DE CONEXIÓN
            // ==========================================

            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message =
                        $"No se pudo conectar con la API: {ex.Message}"
                };
            }

            // ==========================================
            // TIMEOUT
            // ==========================================

            catch (TaskCanceledException)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message =
                        "La solicitud excedió el tiempo de espera."
                };
            }

            // ==========================================
            // TOKEN O CONFIGURACIÓN INVÁLIDA
            // ==========================================

            catch (InvalidOperationException ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ==========================================
        // GET - LISTAR CLIENTES
        // ==========================================

        public Task<ApiResponse<ClienteListadoModel>>
            ListarAsync()
        {
            return EjecutarAsync<ClienteListadoModel>(
                HttpMethod.Get,
                "api/v1/clientes"
            );
        }

        // ==========================================
        // GET - OBTENER CLIENTE
        // ==========================================

        public Task<ApiResponse<ClienteModel>>
            ObtenerAsync(int idCliente)
        {
            return EjecutarAsync<ClienteModel>(
                HttpMethod.Get,
                $"api/v1/clientes/{idCliente}"
            );
        }

        // ==========================================
        // POST - REGISTRAR CLIENTE
        // ==========================================

        public Task<ApiResponse<ClienteModel>>
            RegistrarAsync(ClienteRequest request)
        {
            return EjecutarAsync<ClienteModel>(
                HttpMethod.Post,
                "api/v1/clientes",
                request
            );
        }

        // ==========================================
        // PUT - ACTUALIZAR CLIENTE
        // ==========================================

        public Task<ApiResponse<ClienteModel>>
            ActualizarAsync(
                int idCliente,
                ClienteRequest request)
        {
            return EjecutarAsync<ClienteModel>(
                HttpMethod.Put,
                $"api/v1/clientes/{idCliente}",
                request
            );
        }

        // ==========================================
        // PATCH - ACTIVAR CLIENTE
        // ==========================================

        public Task<ApiResponse<bool>>
            ActivarAsync(int idCliente)
        {
            return EjecutarAsync<bool>(
                HttpMethod.Patch,
                $"api/v1/clientes/{idCliente}/activar"
            );
        }

        // ==========================================
        // PATCH - DESACTIVAR CLIENTE
        // ==========================================

        public Task<ApiResponse<bool>>
            DesactivarAsync(int idCliente)
        {
            return EjecutarAsync<bool>(
                HttpMethod.Patch,
                $"api/v1/clientes/{idCliente}/desactivar"
            );
        }
    }
}