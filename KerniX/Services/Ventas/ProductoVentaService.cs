using System.Text.Json;
using KerniX.Models.Ventas.Venta;

namespace KerniX.Services.Ventas;

public sealed class ProductoVentaService
    : IProductoVentaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductoVentaService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public ProductoVentaService(
        HttpClient httpClient,
        ILogger<ProductoVentaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }


    public async Task<IReadOnlyList<ProductoVentaResponse>>
        BuscarAsync(
            string? termino,
            CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                "api/v1/productos",
                cancellationToken
            );

        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );

        _logger.LogInformation(
            "GET /api/v1/productos -> HTTP {StatusCode}",
            (int)response.StatusCode
        );

        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<ProductoVentaResponse>();
        }

        var productos =
            LeerProductos(json);

        IEnumerable<ProductoVentaResponse> consulta =
            productos.Where(x => x.Estado);

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var texto =
                termino.Trim();

            consulta =
                consulta.Where(
                    producto =>
                        Contiene(
                            producto.Nombre,
                            texto
                        )
                        ||
                        Contiene(
                            producto.Codigo,
                            texto
                        )
                        ||
                        Contiene(
                            producto.CodigoBarras,
                            texto
                        )
                        ||
                        Contiene(
                            producto.Categoria,
                            texto
                        )
                        ||
                        Contiene(
                            producto.Marca,
                            texto
                        )
                );
        }

        return consulta
            .OrderBy(x => x.Nombre)
            .Take(20)
            .ToList();
    }


    public async Task<ProductoVentaResponse?>
        ObtenerPorIdAsync(
            int idProducto,
            CancellationToken cancellationToken = default)
    {
        if (idProducto <= 0)
        {
            return null;
        }

        using var response =
            await _httpClient.GetAsync(
                $"api/v1/productos/{idProducto}",
                cancellationToken
            );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        if (
            root.ValueKind == JsonValueKind.Object
            &&
            root.TryGetProperty(
                "data",
                out var data
            )
            &&
            data.ValueKind == JsonValueKind.Object
        )
        {
            return data.Deserialize<ProductoVentaResponse>(
                JsonOptions
            );
        }

        return null;
    }


    private static List<ProductoVentaResponse>
        LeerProductos(
            string json)
    {
        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        if (
            root.ValueKind != JsonValueKind.Object
        )
        {
            return new();
        }

        if (
            !root.TryGetProperty(
                "data",
                out var data
            )
        )
        {
            return new();
        }

        /*
         API REAL:

         {
             "data": {
                 "items": [...]
             }
         }
        */

        if (
            data.ValueKind == JsonValueKind.Object
            &&
            data.TryGetProperty(
                "items",
                out var items
            )
            &&
            items.ValueKind == JsonValueKind.Array
        )
        {
            return items
                .Deserialize<List<ProductoVentaResponse>>(
                    JsonOptions
                )
                ?? new();
        }

        return new();
    }


    private static bool Contiene(
        string? valor,
        string termino)
    {
        return
            !string.IsNullOrWhiteSpace(valor)
            &&
            valor.Contains(
                termino,
                StringComparison.OrdinalIgnoreCase
            );
    }
}