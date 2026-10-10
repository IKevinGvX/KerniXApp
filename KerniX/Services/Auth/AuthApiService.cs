using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KerniX.DTOs.Auth;
using KerniX.DTOs.Common;

namespace KerniX.Services.Auth;

public sealed class AuthApiService : IAuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiService> _logger;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public AuthApiService(
        HttpClient httpClient,
        ILogger<AuthApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<LoginResponse>?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        // =========================================================
        // REQUEST
        // =========================================================

        _logger.LogInformation(
            "LOGIN -> {BaseUrl}api/v1/auth/login | Usuario: {Usuario}",
            _httpClient.BaseAddress,
            request.Usuario);

        using var response =
            await _httpClient.PostAsJsonAsync(
                "api/v1/auth/login",
                request,
                cancellationToken);

        var contenido =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        // No escribir el cuerpo del login en logs: contiene credenciales derivadas y tokens.
        _logger.LogInformation(
            "LOGIN <- HTTP {StatusCode}",
            (int)response.StatusCode);

        // =========================================================
        // ERROR HTTP
        // =========================================================

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,

                Message = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized =>
                        "Usuario o contraseña incorrectos.",

                    HttpStatusCode.Forbidden =>
                        "Tu usuario no tiene permisos para acceder.",

                    HttpStatusCode.BadRequest =>
                        "Los datos enviados no son válidos.",

                    HttpStatusCode.NotFound =>
                        "No se encontró el servicio de autenticación.",

                    HttpStatusCode.InternalServerError =>
                        "KerniX API presentó un error interno.",

                    _ =>
                        $"KerniX API respondió HTTP {(int)response.StatusCode}."
                }
            };
        }

        // =========================================================
        // BODY VACÍO
        // =========================================================

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message =
                    "La API respondió correctamente pero no devolvió contenido."
            };
        }

        // No aceptar respuestas de autenticación marcadas explícitamente
        // como success:false, aunque accidentalmente incluyan un token.
        try
        {
            using var indicador = JsonDocument.Parse(contenido);
            if (TryGetProperty(indicador.RootElement, "success", out var exito) &&
                exito.ValueKind == JsonValueKind.False)
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "La API rechazó la autenticación."
                };
            }
        }
        catch (JsonException)
        {
            // El parser existente devolverá un error de formato más abajo.
        }

        // =========================================================
        // INTENTO 1
        // ApiResponse<LoginResponse>
        // =========================================================

        try
        {
            var wrapper =
                JsonSerializer.Deserialize<
                    ApiResponse<LoginResponse>>(
                        contenido,
                        JsonOptions);

            if (wrapper is not null &&
                wrapper.Data is not null &&
                !string.IsNullOrWhiteSpace(
                    wrapper.Data.AccessToken))
            {
                wrapper.Success = true;

                /*
                 * NO usamos valores extraños que puedan venir
                 * dentro de Usuario.
                 *
                 * Para el flujo de KerniX el usuario real
                 * proviene del formulario / SQL Server.
                 */
                wrapper.Data.Usuario =
                    request.Usuario;

                return wrapper;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(
                ex,
                "La respuesta no coincide con ApiResponse<LoginResponse>.");
        }

        // =========================================================
        // INTENTO 2
        // LoginResponse DIRECTO
        // =========================================================

        try
        {
            var directo =
                JsonSerializer.Deserialize<LoginResponse>(
                    contenido,
                    JsonOptions);

            if (directo is not null &&
                !string.IsNullOrWhiteSpace(
                    directo.AccessToken))
            {
                directo.Usuario =
                    request.Usuario;

                return new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message =
                        "Inicio de sesión correcto.",
                    Data = directo
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(
                ex,
                "La respuesta no coincide con LoginResponse directo.");
        }

        // =========================================================
        // INTENTO 3
        // LECTURA MANUAL DEL JSON
        // =========================================================

        try
        {
            using var document =
                JsonDocument.Parse(
                    contenido);

            var root =
                document.RootElement;

            string? accessToken = null;
            string? refreshToken = null;

            // =====================================================
            // TOKEN EN RAÍZ
            // =====================================================

            if (TryGetString(
                    root,
                    "accessToken",
                    out var token1))
            {
                accessToken =
                    token1;
            }

            if (string.IsNullOrWhiteSpace(
                    accessToken) &&
                TryGetString(
                    root,
                    "token",
                    out var token2))
            {
                accessToken =
                    token2;
            }

            // =====================================================
            // REFRESH TOKEN EN RAÍZ
            // =====================================================

            if (TryGetString(
                    root,
                    "refreshToken",
                    out var refresh1))
            {
                refreshToken =
                    refresh1;
            }

            // =====================================================
            // DATA
            // =====================================================

            if (TryGetProperty(
                    root,
                    "data",
                    out var dataElement) &&
                dataElement.ValueKind ==
                JsonValueKind.Object)
            {
                if (string.IsNullOrWhiteSpace(
                        accessToken) &&
                    TryGetString(
                        dataElement,
                        "accessToken",
                        out var token3))
                {
                    accessToken =
                        token3;
                }

                if (string.IsNullOrWhiteSpace(
                        accessToken) &&
                    TryGetString(
                        dataElement,
                        "token",
                        out var token4))
                {
                    accessToken =
                        token4;
                }

                if (string.IsNullOrWhiteSpace(
                        refreshToken) &&
                    TryGetString(
                        dataElement,
                        "refreshToken",
                        out var refresh2))
                {
                    refreshToken =
                        refresh2;
                }
            }

            // =====================================================
            // TOKEN EN RESPUESTAS ANIDADAS COMUNES
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                    accessToken))
            {
                accessToken =
                    BuscarStringRecursivo(
                        root,
                        "accessToken");
            }

            if (string.IsNullOrWhiteSpace(
                    accessToken))
            {
                accessToken =
                    BuscarStringRecursivo(
                        root,
                        "token");
            }

            if (string.IsNullOrWhiteSpace(
                    refreshToken))
            {
                refreshToken =
                    BuscarStringRecursivo(
                        root,
                        "refreshToken");
            }

            // =====================================================
            // LOGIN CORRECTO
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    accessToken))
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = true,

                    Message =
                        "Inicio de sesión correcto.",

                    Data =
                        new LoginResponse
                        {
                            AccessToken =
                                accessToken,

                            RefreshToken =
                                refreshToken,

                            /*
                             * MUY IMPORTANTE:
                             *
                             * No intentamos leer "usuario"
                             * del JSON porque tu API está
                             * enviando ahí un objeto completo.
                             *
                             * Usuario real:
                             * admin.kernix
                             *
                             * Lo tomamos del request.
                             */
                            Usuario =
                                request.Usuario
                        }
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Error interpretando el JSON del login.");
        }

        // =========================================================
        // FORMATO NO RECONOCIDO
        // =========================================================

        _logger.LogError(
            "La API respondió HTTP {StatusCode}, pero no se encontró AccessToken.",
            (int)response.StatusCode);

        return new ApiResponse<LoginResponse>
        {
            Success = false,

            Message =
                "La autenticación fue aceptada, " +
                "pero no se encontró el token de acceso."
        };
    }


    // =============================================================
    // REFRESH JWT: ACTIVO SOLO CON ENDPOINT CONFIRMADO
    // =============================================================

    public async Task<ApiResponse<LoginResponse>?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "No se recibió un refresh token."
            };
        }

        string? refreshPath =
            _configuration["ApiSettings:RefreshPath"]?.Trim();

        // Sin ruta confirmada no hacer llamadas a endpoints inventados.
        if (string.IsNullOrWhiteSpace(refreshPath))
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "La renovación JWT todavía no está configurada."
            };
        }

        // Solo rutas relativas al host de ApiSettings:BaseUrl.
        if (!Uri.TryCreate(refreshPath, UriKind.Relative, out _) ||
            refreshPath.StartsWith("//", StringComparison.Ordinal) ||
            refreshPath.Contains("..", StringComparison.Ordinal))
        {
            _logger.LogError("ApiSettings:RefreshPath no es una ruta relativa segura.");
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "La ruta de renovación está mal configurada."
            };
        }

        // Contrato propuesto: { "refreshToken": "..." }.
        // Ajustarlo al DTO real de tu API si utiliza otro formato.
        using var response = await _httpClient.PostAsJsonAsync(
            refreshPath,
            new { refreshToken },
            cancellationToken);

        _logger.LogInformation(
            "REFRESH JWT <- HTTP {StatusCode}",
            (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "La API rechazó la renovación de sesión."
            };
        }

        string contenido = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "La API no devolvió nuevos tokens."
            };
        }

        // Acepta wrapper o respuesta directa, como en LoginAsync.
        // Un success:false explícito nunca debe sobreescribirse.
        try
        {
            using var document = JsonDocument.Parse(contenido);
            JsonElement root = document.RootElement;

            if (TryGetProperty(root, "success", out var ok) &&
                ok.ValueKind == JsonValueKind.False)
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "La API no autorizó la renovación de sesión."
                };
            }

            string? access = BuscarStringRecursivo(root, "accessToken");
            if (string.IsNullOrWhiteSpace(access))
                access = BuscarStringRecursivo(root, "token");

            string? nuevoRefresh = BuscarStringRecursivo(root, "refreshToken");

            if (string.IsNullOrWhiteSpace(access))
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "La API no devolvió un access token válido."
                };
            }

            return new ApiResponse<LoginResponse>
            {
                Success = true,
                Message = "Token renovado.",
                Data = new LoginResponse
                {
                    AccessToken = access,
                    RefreshToken = nuevoRefresh
                }
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inesperado en respuesta de renovación JWT.");
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "La API devolvió un formato de renovación no reconocido."
            };
        }
    }

    // =============================================================
    // BUSCAR PROPIEDAD
    // =============================================================

    private static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement result)
    {
        if (element.ValueKind ==
            JsonValueKind.Object)
        {
            foreach (var property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result =
                        property.Value;

                    return true;
                }
            }
        }

        result =
            default;

        return false;
    }

    // =============================================================
    // OBTENER STRING
    //
    // SOLO acepta JSON string.
    // Nunca convierte objetos JSON completos a texto.
    // =============================================================

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string? value)
    {
        value =
            null;

        if (!TryGetProperty(
                element,
                propertyName,
                out var result))
        {
            return false;
        }

        if (result.ValueKind !=
            JsonValueKind.String)
        {
            return false;
        }

        value =
            result.GetString();

        return !string.IsNullOrWhiteSpace(
            value);
    }

    // =============================================================
    // BÚSQUEDA RECURSIVA
    //
    // Permite localizar accessToken/token aunque la API
    // lo haya colocado dentro de otro objeto.
    // =============================================================

    private static string? BuscarStringRecursivo(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind ==
            JsonValueKind.Object)
        {
            foreach (var property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind ==
                    JsonValueKind.String)
                {
                    var value =
                        property.Value.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            value))
                    {
                        return value;
                    }
                }

                var nested =
                    BuscarStringRecursivo(
                        property.Value,
                        propertyName);

                if (!string.IsNullOrWhiteSpace(
                        nested))
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind ==
            JsonValueKind.Array)
        {
            foreach (var item
                     in element.EnumerateArray())
            {
                var nested =
                    BuscarStringRecursivo(
                        item,
                        propertyName);

                if (!string.IsNullOrWhiteSpace(
                        nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}