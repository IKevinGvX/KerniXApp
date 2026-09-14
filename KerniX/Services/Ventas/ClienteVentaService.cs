using System.Net.Http.Json;
using System.Text.Json;
using KerniX.Models.Ventas.Venta;

namespace KerniX.Services.Ventas;

public sealed class ClienteVentaService
    : IClienteVentaService
{
    private readonly HttpClient _httpClient;

    private readonly ILogger<ClienteVentaService>
        _logger;


    private static readonly JsonSerializerOptions
        JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };


    public ClienteVentaService(
        HttpClient httpClient,
        ILogger<ClienteVentaService> logger)
    {
        _httpClient =
            httpClient;

        _logger =
            logger;
    }


    // =========================================================
    // BUSCAR CLIENTES
    // =========================================================

    public async Task<IReadOnlyList<ClienteVentaResponse>>
        BuscarAsync(
            string? termino,
            CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                "api/v1/clientes",
                cancellationToken
            );


        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );


        _logger.LogInformation(
            "GET /api/v1/clientes -> HTTP {StatusCode}",
            (int)response.StatusCode
        );


        _logger.LogInformation(
            "Respuesta clientes API: {Json}",
            json
        );


        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "La API de clientes respondió HTTP {StatusCode}.",
                (int)response.StatusCode
            );

            return Array.Empty<ClienteVentaResponse>();
        }


        var clientes =
            LeerListaClientes(
                json
            );


        _logger.LogInformation(
            "Clientes obtenidos antes del filtro: {Cantidad}",
            clientes.Count
        );


        IEnumerable<ClienteVentaResponse>
            consulta =
            clientes
                .Where(
                    x => x.Estado
                );


        if (!string.IsNullOrWhiteSpace(termino))
        {
            var texto =
                termino.Trim();


            consulta =
                consulta.Where(
                    cliente =>

                        Contiene(
                            cliente.NombreMostrar,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.NombreCompleto,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.RazonSocial,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.Nombres,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.Apellidos,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.NumeroDocumento,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.Telefono,
                            texto
                        )

                        ||

                        Contiene(
                            cliente.Correo,
                            texto
                        )
                );
        }


        var resultado =
            consulta
                .OrderBy(
                    x => x.NombreMostrar
                )
                .Take(20)
                .ToList();


        _logger.LogInformation(
            "Clientes encontrados para '{Termino}': {Cantidad}",
            termino,
            resultado.Count
        );


        return resultado;
    }


    // =========================================================
    // OBTENER CLIENTE POR ID
    // =========================================================

    public async Task<ClienteVentaResponse?>
        ObtenerPorIdAsync(
            int idCliente,
            CancellationToken cancellationToken = default)
    {
        if (idCliente <= 0)
        {
            return null;
        }


        using var response =
            await _httpClient.GetAsync(
                $"api/v1/clientes/{idCliente}",
                cancellationToken
            );


        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );


        _logger.LogInformation(
            "GET /api/v1/clientes/{IdCliente} -> HTTP {StatusCode}",
            idCliente,
            (int)response.StatusCode
        );


        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "No se pudo obtener el cliente {IdCliente}. Respuesta: {Json}",
                idCliente,
                json
            );

            return null;
        }


        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }


        using var document =
            JsonDocument.Parse(
                json
            );


        var root =
            document.RootElement;


        // API estándar:
        //
        // {
        //   "success": true,
        //   "data": { ... }
        // }

        if (
            root.ValueKind ==
                JsonValueKind.Object

            &&

            TryGetPropertyIgnoreCase(
                root,
                "data",
                out var data
            )

            &&

            data.ValueKind ==
                JsonValueKind.Object
        )
        {
            return data
                .Deserialize<ClienteVentaResponse>(
                    JsonOptions
                );
        }


        // Respuesta directa:
        //
        // {
        //   "idCliente": ...
        // }

        if (
            root.ValueKind ==
                JsonValueKind.Object
        )
        {
            return root
                .Deserialize<ClienteVentaResponse>(
                    JsonOptions
                );
        }


        return null;
    }


    // =========================================================
    // CREAR CLIENTE
    // =========================================================

    public async Task<ClienteVentaResponse?>
        CrearAsync(
            ClienteVentaCrearRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);


        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                "api/v1/clientes",
                request,
                JsonOptions,
                cancellationToken);


        string json =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);


        _logger.LogInformation(
            "POST /api/v1/clientes -> HTTP {StatusCode}",
            (int)response.StatusCode);


        _logger.LogInformation(
            "Respuesta crear cliente API: {Json}",
            json);


        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "No se pudo crear el cliente. HTTP {StatusCode}. Respuesta: {Json}",
                (int)response.StatusCode,
                json);


            string mensaje =
                ObtenerMensajeError(
                    json);


            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(
                    mensaje)
                    ? $"La API respondió HTTP {(int)response.StatusCode}."
                    : mensaje);
        }


        if (string.IsNullOrWhiteSpace(
            json))
        {
            return null;
        }


        using JsonDocument document =
            JsonDocument.Parse(
                json);


        JsonElement root =
            document.RootElement;


        // =====================================================
        // RESPUESTA ESTÁNDAR
        // {
        //   "success": true,
        //   "data": { ... }
        // }
        // =====================================================

        if (
            root.ValueKind ==
                JsonValueKind.Object
            &&
            TryGetPropertyIgnoreCase(
                root,
                "data",
                out JsonElement data)
            &&
            data.ValueKind ==
                JsonValueKind.Object
        )
        {
            return data
                .Deserialize<ClienteVentaResponse>(
                    JsonOptions);
        }


        // =====================================================
        // RESPUESTA DIRECTA
        // {
        //   "idCliente": ...
        // }
        // =====================================================

        if (
            root.ValueKind ==
                JsonValueKind.Object
        )
        {
            return root
                .Deserialize<ClienteVentaResponse>(
                    JsonOptions);
        }


        return null;
    }


    // =========================================================
    // OBTENER MENSAJE DE ERROR API
    // =========================================================

    private static string
        ObtenerMensajeError(
            string json)
    {
        if (string.IsNullOrWhiteSpace(
            json))
        {
            return string.Empty;
        }


        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    json);


            JsonElement root =
                document.RootElement;


            if (
                root.ValueKind ==
                    JsonValueKind.Object
            )
            {
                string[] propiedades =
                {
                    "message",
                    "mensaje",
                    "error",
                    "detail",
                    "title"
                };


                foreach (
                    string propiedad
                    in propiedades)
                {
                    if (
                        TryGetPropertyIgnoreCase(
                            root,
                            propiedad,
                            out JsonElement valor)
                        &&
                        valor.ValueKind ==
                            JsonValueKind.String
                    )
                    {
                        return valor.GetString()
                            ?? string.Empty;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Si no es JSON, devolvemos el texto tal cual.
        }


        return json;
    }


    // =========================================================
    // LEER LISTADO
    // =========================================================

    private List<ClienteVentaResponse>
        LeerListaClientes(
            string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ClienteVentaResponse>();
        }


        try
        {
            using var document =
                JsonDocument.Parse(
                    json
                );


            var root =
                document.RootElement;


            // =================================================
            // CASO 1
            //
            // [
            //   {...},
            //   {...}
            // ]
            // =================================================

            if (
                root.ValueKind ==
                    JsonValueKind.Array
            )
            {
                return DeserializarLista(
                    root
                );
            }


            if (
                root.ValueKind !=
                    JsonValueKind.Object
            )
            {
                return new List<ClienteVentaResponse>();
            }


            // =================================================
            // CASO 2
            //
            // {
            //   "data": [...]
            // }
            // =================================================

            if (
                TryGetPropertyIgnoreCase(
                    root,
                    "data",
                    out var data
                )
            )
            {
                if (
                    data.ValueKind ==
                        JsonValueKind.Array
                )
                {
                    return DeserializarLista(
                        data
                    );
                }


                // =============================================
                // CASO 3
                //
                // {
                //   "data": {
                //      "items": [...]
                //   }
                // }
                // =============================================

                if (
                    data.ValueKind ==
                        JsonValueKind.Object
                )
                {
                    var resultado =
                        BuscarArrayDentroDeObjeto(
                            data
                        );


                    if (resultado.Count > 0)
                    {
                        return resultado;
                    }
                }
            }


            // =================================================
            // CASO 4
            //
            // {
            //   "items": [...]
            // }
            //
            // {
            //   "clientes": [...]
            // }
            //
            // {
            //   "results": [...]
            // }
            // =================================================

            var listaRaiz =
                BuscarArrayDentroDeObjeto(
                    root
                );


            return listaRaiz;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "No se pudo interpretar la respuesta JSON de clientes."
            );


            return new List<ClienteVentaResponse>();
        }
    }


    // =========================================================
    // BUSCAR ARRAY EN OBJETO
    // =========================================================

    private static List<ClienteVentaResponse>
        BuscarArrayDentroDeObjeto(
            JsonElement objeto)
    {
        string[] nombres =
        {
            "items",
            "clientes",
            "results",
            "resultados",
            "records",
            "registros",
            "lista"
        };


        foreach (var nombre in nombres)
        {
            if (
                TryGetPropertyIgnoreCase(
                    objeto,
                    nombre,
                    out var propiedad
                )

                &&

                propiedad.ValueKind ==
                    JsonValueKind.Array
            )
            {
                return DeserializarLista(
                    propiedad
                );
            }
        }


        return new List<ClienteVentaResponse>();
    }


    // =========================================================
    // DESERIALIZAR LISTA
    // =========================================================

    private static List<ClienteVentaResponse>
        DeserializarLista(
            JsonElement elemento)
    {
        return elemento
            .Deserialize<List<ClienteVentaResponse>>(
                JsonOptions
            )

            ?? new List<ClienteVentaResponse>();
    }


    // =========================================================
    // PROPERTY CASE INSENSITIVE
    // =========================================================

    private static bool
        TryGetPropertyIgnoreCase(
            JsonElement elemento,
            string nombre,
            out JsonElement valor)
    {
        foreach (
            var propiedad
            in elemento.EnumerateObject()
        )
        {
            if (
                string.Equals(
                    propiedad.Name,
                    nombre,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                valor =
                    propiedad.Value;

                return true;
            }
        }


        valor =
            default;

        return false;
    }


    // =========================================================
    // FILTRO
    // =========================================================

    private static bool Contiene(
        string? valor,
        string termino)
    {
        if (
            string.IsNullOrWhiteSpace(valor)

            ||

            string.IsNullOrWhiteSpace(termino)
        )
        {
            return false;
        }


        return valor.Contains(
            termino,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
