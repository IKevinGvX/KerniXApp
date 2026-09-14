using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KerniX.Models.Usuarios;

namespace KerniX.Services.Usuarios;

public sealed class UsuarioService
    : IUsuarioService
{
    private readonly HttpClient _httpClient;

    private readonly ILogger<UsuarioService>
        _logger;


    public UsuarioService(
        HttpClient httpClient,
        ILogger<UsuarioService> logger)
    {
        _httpClient =
            httpClient;

        _logger =
            logger;
    }


    // =========================================================
    // OBTENER PERFIL
    // GET /api/v1/usuarios/{idUsuario}
    // =========================================================
    public async Task<UsuarioPerfilResponse?>
    ObtenerPerfilAsync(
        int idUsuario)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    $"api/v1/usuarios/{idUsuario}");


            string contenido =
                await response.Content
                    .ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Error obteniendo usuario {IdUsuario}. Status: {Status}. Respuesta: {Detalle}",
                    idUsuario,
                    response.StatusCode,
                    contenido);

                return null;
            }


            using JsonDocument document =
                JsonDocument.Parse(
                    contenido);


            JsonElement root =
                document.RootElement;


            JsonElement data;

            if (root.TryGetProperty(
                    "data",
                    out JsonElement dataElement) &&
                dataElement.ValueKind != JsonValueKind.Null &&
                dataElement.ValueKind != JsonValueKind.Undefined)
            {
                data =
                    dataElement;
            }
            else
            {
                data =
                    root;
            }


            UsuarioPerfilResponse? perfil =
                data.Deserialize<UsuarioPerfilResponse>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });


            if (perfil is null)
            {
                return null;
            }


            // =====================================================
            // EMPLEADO
            // Soporta:
            // "empleado": "Andrea..."
            // o
            // "nombreCompleto": "Andrea..."
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                perfil.Empleado))
            {
                if (data.TryGetProperty(
                        "nombreCompleto",
                        out JsonElement nombreCompleto))
                {
                    perfil.Empleado =
                        nombreCompleto.GetString()
                        ?? string.Empty;
                }
                else if (data.TryGetProperty(
                             "empleado",
                             out JsonElement empleado))
                {
                    perfil.Empleado =
                        empleado.GetString()
                        ?? string.Empty;
                }
            }


            return perfil;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo perfil usuario {IdUsuario}",
                idUsuario);

            return null;
        }
    }
    // =========================================================
    // ACTUALIZAR PERFIL
    // PUT /api/v1/usuarios/{idUsuario}
    // =========================================================
    public async Task<ApiOperationResult> ActualizarPerfilAsync(
    int idUsuario,
    ActualizarUsuarioRequest request)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(
                    $"api/v1/usuarios/{idUsuario}",
                    request);

            string contenido =
                await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "PUT usuario {IdUsuario} -> {Status}. BODY: {Body}",
                idUsuario,
                response.StatusCode,
                contenido);

            if (response.IsSuccessStatusCode)
            {
                return new ApiOperationResult
                {
                    Success = true,
                    Message = "Perfil actualizado correctamente."
                };
            }

            string mensaje =
                ExtraerMensajeApi(contenido);

            return new ApiOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(mensaje)
                    ? $"La API respondió {(int)response.StatusCode}."
                    : mensaje
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error actualizando usuario {IdUsuario}.",
                idUsuario);

            return new ApiOperationResult
            {
                Success = false,
                Message = "No se pudo conectar con la API."
            };
        }
    }


    // =========================================================
    // CAMBIAR CONTRASEÑA
    // PATCH /api/v1/usuarios/{idUsuario}/cambiar-password
    // =========================================================

    public async Task<ApiOperationResult> CambiarPasswordAsync(
     int idUsuario,
     CambiarPasswordRequest request)
    {
        try
        {
            using HttpRequestMessage message =
                new(
                    HttpMethod.Patch,
                    $"api/v1/usuarios/{idUsuario}/cambiar-password")
                {
                    Content = JsonContent.Create(request)
                };

            using HttpResponseMessage response =
                await _httpClient.SendAsync(message);

            string contenido =
                await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "PATCH password usuario {IdUsuario} -> {Status}. BODY: {Body}",
                idUsuario,
                response.StatusCode,
                contenido);

            if (response.IsSuccessStatusCode)
            {
                return new ApiOperationResult
                {
                    Success = true,
                    Message = "Contraseña actualizada correctamente."
                };
            }

            string mensaje =
                ExtraerMensajeApi(contenido);

            return new ApiOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(mensaje)
                    ? $"La API respondió {(int)response.StatusCode}."
                    : mensaje
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cambiando contraseña usuario {IdUsuario}.",
                idUsuario);

            return new ApiOperationResult
            {
                Success = false,
                Message = "No se pudo conectar con la API."
            };
        }

    }
    private static string ExtraerMensajeApi(
    string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(contenido);

            JsonElement root =
                document.RootElement;

            if (root.TryGetProperty(
                    "message",
                    out JsonElement message))
            {
                return message.GetString()
                       ?? string.Empty;
            }
        }
        catch
        {
            // Ignorar JSON inválido.
        }

        return contenido;
    }
}
