using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KerniX.Models.IA;
using KerniX.Services.IA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.IA;

/// <summary>
/// Fase 5: Ollama interpreta la intención; solo el servidor valida empresa
/// y ejecuta la herramienta permitida, lectura del catálogo SQL.
/// Ninguna instrucción generada por IA ejecuta SQL o modifica ventas.
/// </summary>
public sealed class AgenteVentasModel : IaPageModelBase
{
    private readonly IAgenteCatalogoService _catalogo;
    private readonly IConversacionIaService _conversacion;
    private readonly ILogger<AgenteVentasModel> _logger;
    private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");

    public AgenteVentasModel(
        IKerniXIaService ia,
        IAgenteCatalogoService catalogo,
        IConversacionIaService conversacion,
        ILogger<AgenteVentasModel> logger) : base(ia, logger)
    {
        _catalogo = catalogo;
        _conversacion = conversacion;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var invalid = await ValidarContextoAsync(cancellationToken);
        return invalid ?? Page();
    }

    // Buscador manual existente. No cambia su contrato HTTP ni su interfaz.
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostBuscarAsync(
        [FromForm] string? consulta, CancellationToken cancellationToken)
    {
        var invalid = await ValidarContextoAsync(cancellationToken);
        if (invalid is not null || Contexto is null)
            return Error(401, "Sesión expirada. Inicia sesión nuevamente.");

        string texto = consulta?.Trim() ?? "";
        if (texto.Length is < 2 or > 120)
            return Error(400, "Escribe entre 2 y 120 caracteres para buscar.");

        try
        {
            var items = await _catalogo.BuscarAsync(Contexto.IdEmpresa, texto, cancellationToken);
            return new JsonResult(new
            {
                ok = true,
                items,
                mensaje = items.Count == 0 ? "No se encontraron productos." : "Catálogo consultado correctamente.",
                consultadoUtc = DateTimeOffset.UtcNow
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Error SQL buscando catálogo. Empresa={Empresa}", Contexto.IdEmpresa);
            return Error(500, "No se pudo consultar SQL Server. Revisa Salida en Visual Studio.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Esquema incompatible.");
            return Error(500, "El esquema SQL de catálogo no coincide con el servicio.");
        }
    }

    // Fase 5: validar y valorar un BORRADOR, sin insertar cotizaciones ni reservar stock.
    // El cliente envía SOLO idProducto + cantidad; nombre, precio y stock se releen del SQL.
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostValidarBorradorAsync(
        [FromForm] string? detalle,
        [FromForm] string? cliente,
        [FromForm] string? notas,
        CancellationToken cancellationToken)
    {
        var invalid = await ValidarContextoAsync(cancellationToken);
        if (invalid is not null || Contexto is null)
            return Error(401, "La sesión expiró o no tiene empresa válida.");

        if (detalle is null || detalle.Length is < 2 or > 6000)
            return Error(400, "El detalle del borrador no es válido.");
        if ((cliente?.Length ?? 0) > 100 || (notas?.Length ?? 0) > 450)
            return Error(400, "Cliente u observaciones exceden el tamaño permitido.");

        try
        {
            using var json = JsonDocument.Parse(detalle);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
                return Error(400, "El detalle debe ser una lista de productos.");
            var lineas = new List<(int Id, int Cantidad)>();
            foreach (var elem in json.RootElement.EnumerateArray())
            {
                if (elem.ValueKind != JsonValueKind.Object ||
                    !elem.TryGetProperty("idProducto", out var idProp) ||
                    !elem.TryGetProperty("cantidad", out var qtyProp) ||
                    idProp.ValueKind != JsonValueKind.Number || qtyProp.ValueKind != JsonValueKind.Number ||
                    !idProp.TryGetInt32(out int id) || !qtyProp.TryGetInt32(out int cantidad) ||
                    id <= 0 || cantidad is < 1 or > 9999)
                    return Error(400, "Cada línea debe contener un idProducto y cantidad válidos.");
                lineas.Add((id, cantidad));
                if (lineas.Count > 18)
                    return Error(400, "El borrador admite como máximo 18 productos diferentes.");
            }
            if (lineas.Count == 0 || lineas.Select(x => x.Id).Distinct().Count() != lineas.Count)
                return Error(400, "El borrador está vacío o contiene productos duplicados.");

            var catalogo = await _catalogo.ObtenerPorIdsAsync(
                Contexto.IdEmpresa, lineas.Select(x => x.Id).ToArray(), cancellationToken);
            var actuales = catalogo.ToDictionary(p => p.IdProducto);
            var advertencias = new List<string>();
            var respuesta = new List<object>();
            decimal total = 0m;
            bool tieneTodosLosPrecios = true;
            var texto = new StringBuilder();
            texto.AppendLine("KERNIX ERP — BORRADOR COMERCIAL REVISADO EN SQL");
            texto.AppendLine("ESTADO: PENDIENTE DE APROBACIÓN HUMANA · NO REGISTRADO");
            texto.AppendLine($"Referencia: {(string.IsNullOrWhiteSpace(cliente) ? "Sin identificar" : cliente.Trim())}");
            texto.AppendLine($"Fecha de validación UTC: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
            texto.AppendLine("DETALLE:");

            for (int i = 0; i < lineas.Count; i++)
            {
                var (id, cantidad) = lineas[i];
                if (!actuales.TryGetValue(id, out var producto))
                {
                    // Fail closed: se ha desactivado, eliminado o no pertenece a esta empresa.
                    return Error(409, $"El producto #{id} dejó de estar disponible para esta empresa. Actualiza el catálogo.");
                }
                decimal? precio = producto.PrecioVenta;
                decimal? disponible = producto.StockDisponible;
                if (precio is null || precio < 0)
                {
                    tieneTodosLosPrecios = false;
                    advertencias.Add($"{producto.Nombre}: precio no verificable.");
                }
                if (disponible is null)
                    advertencias.Add($"{producto.Nombre}: stock no verificable.");
                else if (disponible.Value < cantidad)
                    advertencias.Add($"{producto.Nombre}: solicitas {cantidad} y la disponibilidad consultada es {disponible.Value:N2}.");

                decimal? importe = precio is >= 0 ?
                    Math.Round(precio.Value * cantidad, 2, MidpointRounding.AwayFromZero) : null;
                if (importe.HasValue) total = checked(total + importe.Value);
                respuesta.Add(new
                {
                    idProducto = producto.IdProducto,
                    codigo = producto.Codigo,
                    nombre = producto.Nombre,
                    cantidad,
                    precioUnitario = precio,
                    stockDisponible = disponible,
                    importeReferencial = importe
                });
                texto.AppendLine($"{i + 1}. {producto.Nombre} [{producto.Codigo}] x {cantidad} · Precio: {(precio is null ? "Pendiente" : $"S/ {precio.Value:N2}")} · Importe: {(importe is null ? "Pendiente" : $"S/ {importe.Value:N2}")}");
            }
            decimal? importeTotal = tieneTodosLosPrecios ? total : null;
            texto.AppendLine($"IMPORTE REFERENCIAL: {(importeTotal is null ? "PENDIENTE" : $"S/ {importeTotal.Value:N2}")}");
            texto.AppendLine($"Observaciones: {(string.IsNullOrWhiteSpace(notas) ? "Sin observaciones" : notas.Trim())}");
            foreach (string advertencia in advertencias)
                texto.AppendLine($"ADVERTENCIA: {advertencia}");
            texto.AppendLine("Los importes usan precio_venta de SQL. Revisar IGV, descuentos, moneda, entregas, cliente, precios y stock al registrar.");
            texto.AppendLine("NO se creó cotización, pedido, venta, reserva, pago ni comprobante.");

            _logger.LogInformation("[KERNIX IA] Borrador verificado. Usuario={Usuario} Empresa={Empresa} Lineas={Lineas} Advertencias={Advertencias}",
                Contexto.IdUsuario, Contexto.IdEmpresa, lineas.Count, advertencias.Count);
            return new JsonResult(new
            {
                ok = true,
                estado = "PENDIENTE_REVISION_HUMANA",
                registrado = false,
                aprobado = false,
                origen = "KerniX SQL Server",
                lineas = respuesta,
                importeReferencial = importeTotal,
                advertencias,
                textoBorrador = texto.ToString(),
                validadoUtc = DateTimeOffset.UtcNow
            });
        }
        catch (JsonException) { return Error(400, "El detalle no contiene JSON válido."); }
        catch (OverflowException) { return Error(400, "El importe supera el límite admitido."); }
        catch (ArgumentException) { return Error(400, "Los productos seleccionados no son válidos."); }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Error SQL revalidando borrador. Empresa={Empresa}", Contexto.IdEmpresa);
            return Error(500, "No fue posible revalidar precios o existencias en SQL Server.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Esquema no compatible con cotización asistida.");
            return Error(500, "El catálogo SQL no contiene los campos necesarios.");
        }
    }

    // Chat único: Ollama conversa y, cuando procede, clasifica una consulta de catálogo.
    // La decisión final y la consulta siempre ocurren EN EL SERVIDOR web.
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostConversarAsync(
        [FromForm] string? mensaje, CancellationToken cancellationToken)
    {
        var invalid = await ValidarContextoAsync(cancellationToken);
        if (invalid is not null || Contexto is null)
            return Error(401, "Sesión expirada. Inicia sesión nuevamente.");

        string texto = mensaje?.Trim() ?? "";
        if (texto.Length is < 2 or > 1500)
            return Error(400, "Escribe un mensaje de 2 a 1500 caracteres.");

        try
        {
            SolicitudCatalogo? solicitud = DetectarCatalogo(texto);
            if (solicitud is null && !EsSaludoSimple(texto))
                solicitud = await InterpretarConOllamaAsync(texto, cancellationToken);

            if (solicitud is not null)
            {
                IReadOnlyList<AgenteCatalogoProducto> productos =
                    string.IsNullOrWhiteSpace(solicitud.Termino)
                        ? await _catalogo.ListarAsync(Contexto.IdEmpresa, cancellationToken)
                        : await _catalogo.BuscarAsync(Contexto.IdEmpresa, solicitud.Termino, cancellationToken);

                string explicacion = ConstruirRespuestaCatalogo(productos, solicitud);
                _logger.LogInformation(
                    "[KERNIX IA] Herramienta catálogo SOLO LECTURA Usuario={Usuario} Empresa={Empresa} Resultados={N}",
                    Contexto.IdUsuario, Contexto.IdEmpresa, productos.Count);

                return new JsonResult(new
                {
                    ok = true,
                    respuesta = explicacion,
                    proveedor = "KerniX SQL",
                    modelo = "Consulta verificada",
                    consultoCatalogo = true,
                    consulta = solicitud.Termino ?? "",
                    cantidadSolicitada = solicitud.Cantidad,
                    sugerenciaBorrador = EsPeticionDeCotizacion(texto) &&
                        solicitud.Cantidad is >= 1 and <= 9999 && productos.Count == 1 &&
                        productos[0].PrecioVenta is >= 0 &&
                        productos[0].StockDisponible is not null &&
                        productos[0].StockDisponible >= solicitud.Cantidad
                            ? new { idProducto = productos[0].IdProducto, cantidad = solicitud.Cantidad }
                            : null,
                    items = productos,
                    consultadoUtc = DateTimeOffset.UtcNow,
                    operacionesEjecutadas = false // Ninguna operación de ESCRITURA.
                });
            }

            var respuesta = await _conversacion.ConversarAsync(texto, cancellationToken);
            if (!respuesta.Ok)
                return Error(respuesta.CodigoHttp, respuesta.Mensaje);

            return new JsonResult(new
            {
                ok = true,
                respuesta = respuesta.Mensaje,
                proveedor = respuesta.Proveedor,
                modelo = respuesta.Modelo,
                consultoCatalogo = false,
                operacionesEjecutadas = false
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Error de lectura SQL. Empresa={Empresa}", Contexto.IdEmpresa);
            return Error(500, "No fue posible consultar el catálogo real. Revisa el error SQL en Visual Studio.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[KERNIX IA] Error de esquema al consultar productos.");
            return Error(500, "El esquema de productos no es compatible con el servicio. No se han inventado resultados.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "[KERNIX IA] Consulta inválida.");
            return Error(400, "La consulta del catálogo no es válida. Intenta con un nombre o código de producto.");
        }
    }

    private async Task<SolicitudCatalogo?> InterpretarConOllamaAsync(
        string mensaje, CancellationToken ct)
    {
        // Ollama solo propone una acción, NUNCA da nombres de tablas, SQL, usuarios ni empresa.
        // La API inicial admite hasta 2000 caracteres: enviamos un fragmento limitado.
        string fragmento = mensaje[..Math.Min(450, mensaje.Length)];
        string instruccion = "Clasifica el mensaje para un agente de ventas. " +
            "Contesta SOLO JSON con las claves accion, termino, cantidad. " +
            "accion debe ser catalogo si pide consultar/listar/buscar productos, precio o stock; " +
            "en otro caso conversar. termino es el NOMBRE o CODIGO del producto a buscar, " +
            "o cadena vacia para listar el catalogo. cantidad es un entero o 0. " +
            "No inventes productos. No agregues texto antes o despues del JSON. " +
            "Ejemplo: {\"accion\":\"catalogo\",\"termino\":\"laptop\",\"cantidad\":5}. " +
            "Mensaje del usuario: " + fragmento;

        var clasificacion = await _conversacion.ConversarAsync(instruccion, ct);
        if (!clasificacion.Ok) return null; // El modo conversacional normal presentará su propio error.
        try
        {
            string raw = clasificacion.Mensaje;
            int inicio = raw.IndexOf('{');
            int fin = raw.LastIndexOf('}');
            if (inicio < 0 || fin <= inicio) return null;
            using var json = JsonDocument.Parse(raw.Substring(inicio, fin - inicio + 1));
            var root = json.RootElement;
            if (!root.TryGetProperty("accion", out var accion) ||
                !string.Equals(accion.GetString(), "catalogo", StringComparison.OrdinalIgnoreCase))
                return null;

            string termino = root.TryGetProperty("termino", out var q) && q.ValueKind == JsonValueKind.String
                ? q.GetString() ?? "" : "";
            termino = ExtraerTermino(termino);
            int cantidad = 0;
            if (root.TryGetProperty("cantidad", out var c) && c.ValueKind == JsonValueKind.Number)
                c.TryGetInt32(out cantidad);

            return new SolicitudCatalogo(termino, cantidad is >= 1 and <= 9999 ? cantidad : null);
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    private static SolicitudCatalogo? DetectarCatalogo(string mensaje)
    {
        string normal = Normalizar(mensaje);
        // Preguntas conceptuales no son peticiones de lectura de catálogo.
        if (Regex.IsMatch(normal,
            @"\b(que es|como funciona|como se|que significa|explicame|definicion|concepto)\b"))
            return null;
        bool palabraCatalogo = Regex.IsMatch(normal,
            @"\b(productos?|catalogo|inventario|existencias?|stock|precios?)\b");
        bool verbosComerciales = Regex.IsMatch(normal,
            @"\b(busca|buscar|muestra|mostrar|muestrame|mostrarme|ensena|ensenar|lista|listar|dame|darme|tienes|tienen|vendes|disponibles?|cotizar|cotizame|cotiza|cotizacion|comprar|compro|quiero|necesito|ofreces|cuanto|cuesta|cuestan)\b");
        bool categoriaHabitual = Regex.IsMatch(normal,
            @"\b(laptops?|computadoras?|mouse|teclados?|monitores?|impresoras?|cables?|discos?|routers?|telefonos?|celulares?)\b");
        bool tieneCodigoProducto = Regex.IsMatch(mensaje, @"\b[A-Za-z][A-Za-z0-9]*-[A-Za-z0-9-]{2,}\b");
        if (!palabraCatalogo && !(verbosComerciales && (categoriaHabitual || tieneCodigoProducto))) return null;

        // No interpretar la parte numérica de PROD-001 como cantidad.
        string sinCodigos = Regex.Replace(normal, @"\b[a-z][a-z0-9]*-[a-z0-9-]{2,}\b", " ");
        var cantidad = Regex.Match(sinCodigos, @"\b([1-9][0-9]{0,3})\b");
        int? numero = cantidad.Success && int.TryParse(cantidad.Value, out int n) ? n : null;
        string termino = ExtraerTermino(mensaje);
        return new SolicitudCatalogo(termino, numero);
    }

    private static string ExtraerTermino(string mensaje)
    {
        // Códigos completos tienen prioridad: evita confundir PROD-001 con cantidad 1.
        var codigo = Regex.Match(mensaje, @"\b[A-Za-z][A-Za-z0-9]*-[A-Za-z0-9-]{2,}\b");
        if (codigo.Success) return LimpiarBusqueda(codigo.Value);
        var palabrasVacias = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hola", "por", "favor", "quiero", "quisiera", "necesito", "dame", "muestra", "muestrame",
            "ensena", "busca", "buscar", "lista", "listar", "ver", "tus", "tienen", "tienes", "darme", "dime", "indica", "indicame", "mostrar", "mostrarme",
            "todos", "todo", "todas", "los", "las", "un", "una", "unos", "unas", "de", "del", "a", "al",
            "en", "mi", "para", "con", "que", "cuales", "cual", "es", "hay", "son", "sus", "el", "la", "cuanto", "cuantos", "cuesta", "cuestan", "vale", "valen", "valor",
            "me", "te", "se", "si", "puedes", "puede", "podrias", "porfavor", "vendiendo",
            "producto", "productos", "catalogo", "inventario", "venta", "ventas", "stock", "existencia", "tienda", "listado",
            "existencias", "disponible", "disponibles", "precio", "precios", "costo", "costos", "cotizacion",
            "cotizar", "comprar", "compra", "ofreces", "vendes", "empresa", "negocio", "comercial",
            "economico", "economicos", "economicas", "economica", "mas", "menos", "hoy", "ahora", "actualmente", "soles", "sol", "presupuesto", "marca", "modelo",
            "kerni", "kernix", "ia", "quiero", "pedido", "cantidad", "unidades", "unidad", "necesita", "necesitan"
        };
        var palabras = Regex.Matches(Normalizar(mensaje), @"[\p{L}\p{N}][\p{L}\p{N}._-]*")
            .Select(x => x.Value)
            .Where(x => x.Length >= 2 && !palabrasVacias.Contains(x) && !int.TryParse(x, out _))
            .Take(5)
            .ToList();
        if (palabras.Count == 0) return "";
        // El usuario puede escribir 'laptops', mientras el producto es 'laptop'.
        string candidato = palabras[0];
        if (candidato.Length > 5 && candidato.EndsWith('s') && !candidato.EndsWith("ss", StringComparison.Ordinal))
            candidato = candidato[..^1];
        return LimpiarBusqueda(candidato);
    }

    private static string Normalizar(string input)
    {
        string forma = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return Regex.Replace(forma, @"\p{Mn}", "").Normalize(NormalizationForm.FormC);
    }

    private static string LimpiarBusqueda(string consulta)
    {
        string resultado = Regex.Replace(consulta, @"[^\p{L}\p{N} ._-]+", " ").Trim();
        if (resultado.Length > 120) resultado = resultado[..120].Trim();
        // Una sola letra no sirve como término de búsqueda (nuestra API manual exige al menos 2).
        return resultado.Length < 2 ? "" : resultado;
    }

    private static bool EsPeticionDeCotizacion(string texto) => Regex.IsMatch(
        Normalizar(texto), @"\b(cotiza|cotizame|cotizacion|propuesta|presupuesto)\b");

    private static bool EsSaludoSimple(string mensaje)
    {
        string normal = Normalizar(mensaje).Trim(' ', '.', '!', '?', '¡', '¿');
        return normal is "hola" or "buenas" or "gracias" or "adios" or "buenos dias" or "buenas tardes";
    }

    private static string ConstruirRespuestaCatalogo(
        IReadOnlyList<AgenteCatalogoProducto> productos, SolicitudCatalogo solicitud)
    {
        string q = string.IsNullOrWhiteSpace(solicitud.Termino)
            ? "del catálogo activo" : $"relacionados con «{solicitud.Termino}»";
        if (productos.Count == 0)
            return $"Consulté SQL Server para buscar productos {q}, pero no se encontraron coincidencias. " +
                   "Prueba con otro nombre o código. No se ha registrado ninguna operación.";

        var sb = new StringBuilder();
        sb.AppendLine($"Consulta verificada en KerniX SQL: muestro {productos.Count} producto(s) {q} (máximo 18 por consulta).");
        sb.AppendLine("Listado verificado (hasta 8 aquí; revisa la lista completa debajo):");
        foreach (var p in productos.Take(8))
        {
            string precio = p.PrecioVenta is null ? "precio no disponible" : $"S/ {p.PrecioVenta.Value.ToString("N2", Peru)}";
            string stock = p.StockDisponible is null ? "stock no verificado" : $"stock consultado {p.StockDisponible.Value.ToString("N2", Peru)}";
            sb.AppendLine($"• {p.Nombre} [{p.Codigo}] — {precio}; {stock}.");
        }
        if (solicitud.Cantidad is not null)
            sb.AppendLine($"Cantidad solicitada: {solicitud.Cantidad}. Verifica la disponibilidad por producto antes de añadirla.");
        sb.AppendLine("Puedes añadir productos a la propuesta comercial. Los precios y existencias son una fotografía de la consulta; no hay reservas ni cotización oficial registrada.");
        return sb.ToString();
    }

    private static JsonResult Error(int code, string msg) =>
        new(new { ok = false, mensaje = msg }) { StatusCode = code };

    private sealed record SolicitudCatalogo(string Termino, int? Cantidad);
}
