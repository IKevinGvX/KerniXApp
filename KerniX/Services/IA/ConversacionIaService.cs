using System.Net.Http.Json;
using System.Text.Json;
using KerniX.Models.IA;

namespace KerniX.Services.IA;

/// <summary>
/// Servidor Web -> KerniX.IA.Api. El secreto NUNCA sale al navegador.
/// No envía ids de empresa, JWT ni información de productos al modelo.
/// </summary>
public sealed class ConversacionIaService : IConversacionIaService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ConversacionIaService> _logger;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ConversacionIaService(
        HttpClient http,
        IConfiguration config,
        ILogger<ConversacionIaService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<ResultadoConversacionIa> ConversarAsync(
        string mensaje, CancellationToken cancellationToken = default)
    {
        string? clave = _config["IaMotor:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave))
            return new(false, "Falta la clave de conexión IA en KerniX Web. Configura IaMotor:ClaveInterna en User Secrets.", 503);

        if (_http.BaseAddress is null)
            return new(false, "Falta IaMotor:BaseUrl en la configuración web.", 503);

        try
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, "api/ia/conversar")
            {
                Content = JsonContent.Create(new { mensaje })
            };
            peticion.Headers.TryAddWithoutValidation("X-KerniX-IA-Key", clave);

            using var respuesta = await _http.SendAsync(
                peticion, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                int codigo = (int)respuesta.StatusCode;
                _logger.LogWarning("[KERNIX IA WEB] API IA respondió HTTP={Codigo}", codigo);
                var mensajeError = codigo switch
                {
                    401 => "La API IA rechazó la clave interna. Comprueba que ambos proyectos tengan la misma clave.",
                    400 => "El motor IA rechazó el mensaje enviado.",
                    502 => "Ollama no respondió correctamente a KerniX.IA.Api.",
                    503 => "El motor IA no está disponible o no está configurado.",
                    504 => "La generación del modelo superó el tiempo de espera.",
                    _ => "No fue posible obtener una respuesta del motor IA."
                };
                return new(false, mensajeError, codigo is >= 400 and < 600 ? codigo : 502);
            }

            await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
            var datos = await JsonSerializer.DeserializeAsync<RespuestaModelo>(flujo, Json, cancellationToken);
            if (datos is null || string.IsNullOrWhiteSpace(datos.Respuesta))
                return new(false, "La API IA respondió sin texto utilizable.", 502);

            return new(true, datos.Respuesta, 200,
                datos.Proveedor ?? "Ollama", datos.Modelo ?? "");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "[KERNIX IA WEB] Tiempo de espera agotado.");
            return new(false, "La IA tardó demasiado. Intenta de nuevo en unos segundos.", 504);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[KERNIX IA WEB] No se pudo contactar a KerniX.IA.Api.");
            return new(false, "No se pudo conectar a KerniX.IA.Api. Comprueba el puerto 5217.", 502);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[KERNIX IA WEB] JSON de IA inválido.");
            return new(false, "La API IA devolvió una respuesta con formato inesperado.", 502);
        }
    }

    private sealed class RespuestaModelo
    {
        public string? Respuesta { get; set; }
        public string? Proveedor { get; set; }
        public string? Modelo { get; set; }
    }
}
