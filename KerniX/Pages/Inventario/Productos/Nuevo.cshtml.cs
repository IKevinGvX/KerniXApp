using System.Globalization;

using KerniX.Models.Inventario.Producto;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;

namespace KerniX.Pages.Inventario.Productos;

public sealed class NuevoModel : PageModel
{
    private readonly IProductoDbService _productoService;
    private readonly ILogger<NuevoModel> _logger;

    public NuevoModel(
        IProductoDbService productoService,
        ILogger<NuevoModel> logger)
    {
        _productoService = productoService;
        _logger = logger;
    }

    [BindProperty]
    public ProductoGuardarRequest Input { get; set; } = CrearInputInicial();

    public ProductoCatalogos Catalogos { get; private set; } = new();

    public int IdUsuario { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }

    public string? MensajeError { get; private set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await CargarCatalogosAsync(cancellationToken);

            Input = CrearInputInicial();
            Input.IdEmpresa = IdEmpresa;
            Input.IdUsuario = IdUsuario;

            _logger.LogInformation(
                "[PRODUCTO NUEVO] GET. Usuario={Usuario} Empresa={Empresa}",
                IdUsuario,
                IdEmpresa);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTO NUEVO] Error cargando formulario.");

            MensajeError =
                "No fue posible cargar los datos necesarios para registrar el producto.";

            return Page();
        }
    }


    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PRODUCTO NUEVO] POST recibido.");

        if (!CargarContextoSesion())
        {
            _logger.LogWarning("[PRODUCTO NUEVO] POST sin sesión válida.");
            return RedirectToPage("/Account/Login");
        }

        try
        {
            if (!Request.HasFormContentType)
            {
                throw new InvalidOperationException(
                    "La solicitud no contiene datos de formulario.");
            }

            IFormCollection form =
                await Request.ReadFormAsync(cancellationToken);

            _logger.LogInformation(
                "[PRODUCTO NUEVO] Campos recibidos: {Campos}",
                string.Join(", ", form.Keys));

            // =====================================================
            // IMPORTANTE
            // Leemos Request.Form manualmente.
            // Así no dependemos del model binder para este formulario.
            // =====================================================

            Input = new ProductoGuardarRequest
            {
                IdEmpresa = IdEmpresa,
                IdUsuario = IdUsuario,

                Codigo = LeerTexto(form, "Input.Codigo"),
                CodigoBarras = LeerTextoNullable(form, "Input.CodigoBarras"),
                Nombre = LeerTexto(form, "Input.Nombre"),
                Descripcion = LeerTextoNullable(form, "Input.Descripcion"),

                IdCategoriaProducto = LeerInt(form, "Input.IdCategoriaProducto"),
                IdMarca = LeerIntNullable(form, "Input.IdMarca"),
                IdUnidadMedida = LeerInt(form, "Input.IdUnidadMedida"),

                PrecioCompra = LeerDecimal(form, "Input.PrecioCompra"),
                PrecioVenta = LeerDecimal(form, "Input.PrecioVenta"),

                StockMinimo = LeerDecimal(form, "Input.StockMinimo"),
                StockMaximo = LeerDecimalNullable(form, "Input.StockMaximo"),

                ControlaStock = LeerBool(form, "Input.ControlaStock")
            };

            if (!Input.ControlaStock)
            {
                Input.StockMinimo = 0M;
                Input.StockMaximo = null;
            }

            // El binder pudo haber agregado errores antes de que leyéramos
            // el formulario manualmente. Limpiamos y validamos nosotros.
            ModelState.Clear();

            _logger.LogInformation(
                "[PRODUCTO NUEVO] Datos POST. Usuario={Usuario} Empresa={Empresa} Categoria={Categoria} Marca={Marca} Unidad={Unidad} Codigo={Codigo} Nombre={Nombre} Compra={Compra} Venta={Venta} Minimo={Minimo} Maximo={Maximo} ControlStock={ControlStock}",
                IdUsuario,
                IdEmpresa,
                Input.IdCategoriaProducto,
                Input.IdMarca,
                Input.IdUnidadMedida,
                Input.Codigo,
                Input.Nombre,
                Input.PrecioCompra,
                Input.PrecioVenta,
                Input.StockMinimo,
                Input.StockMaximo,
                Input.ControlaStock);

            ValidarFormulario();

            if (!ModelState.IsValid)
            {
                LogModelState();

                await CargarCatalogosAsync(cancellationToken);

                MensajeError =
                    "Existen datos inválidos. Revisa los mensajes mostrados en el formulario.";

                return Page();
            }

            _logger.LogInformation(
                "[PRODUCTO NUEVO] Ejecutando CrearAsync. Codigo={Codigo}",
                Input.Codigo);

            int idProducto =
                await _productoService.CrearAsync(
                    Input,
                    cancellationToken);

            if (idProducto <= 0)
            {
                throw new InvalidOperationException(
                    "El registro terminó sin devolver un identificador válido.");
            }

            _logger.LogInformation(
                "[PRODUCTO NUEVO] Producto creado correctamente. Id={Producto} Codigo={Codigo} Usuario={Usuario} Empresa={Empresa}",
                idProducto,
                Input.Codigo,
                IdUsuario,
                IdEmpresa);

            TempData["ProductoExito"] =
                "Producto registrado correctamente.";

            return RedirectToPage(
                "./Detalle",
                new
                {
                    idProducto
                });
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTO NUEVO] ERROR SQL. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea} Mensaje={Mensaje} Usuario={Usuario} Empresa={Empresa} Codigo={Codigo}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber,
                ex.Message,
                IdUsuario,
                IdEmpresa,
                Input.Codigo);

            await CargarCatalogosAsync(cancellationToken);

            MensajeError =
                $"SQL {ex.Number} | " +
                $"Procedimiento: {ex.Procedure ?? "(sin procedimiento)"} | " +
                $"Línea: {ex.LineNumber} | " +
                ex.Message;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTO NUEVO] Error general registrando producto. Codigo={Codigo}",
                Input.Codigo);

            await CargarCatalogosAsync(cancellationToken);

            MensajeError = ex.Message;
            return Page();
        }
    }


    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private void ValidarFormulario()
    {
        if (Input.IdCategoriaProducto <= 0)
        {
            ModelState.AddModelError(
                "Input.IdCategoriaProducto",
                "Selecciona una categoría.");
        }

        if (Input.IdUnidadMedida <= 0)
        {
            ModelState.AddModelError(
                "Input.IdUnidadMedida",
                "Selecciona una unidad de medida.");
        }

        if (string.IsNullOrWhiteSpace(Input.Codigo))
        {
            ModelState.AddModelError(
                "Input.Codigo",
                "El código del producto es obligatorio.");
        }
        else if (Input.Codigo.Length > 100)
        {
            ModelState.AddModelError(
                "Input.Codigo",
                "El código no puede superar los 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(Input.Nombre))
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "El nombre del producto es obligatorio.");
        }
        else if (Input.Nombre.Length > 400)
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "El nombre no puede superar los 400 caracteres.");
        }

        if (Input.CodigoBarras?.Length > 200)
        {
            ModelState.AddModelError(
                "Input.CodigoBarras",
                "El código de barras no puede superar los 200 caracteres.");
        }

        if (Input.Descripcion?.Length > 1000)
        {
            ModelState.AddModelError(
                "Input.Descripcion",
                "La descripción no puede superar los 1000 caracteres.");
        }

        if (Input.PrecioCompra < 0)
        {
            ModelState.AddModelError(
                "Input.PrecioCompra",
                "El precio de compra no puede ser negativo.");
        }

        if (Input.PrecioVenta < 0)
        {
            ModelState.AddModelError(
                "Input.PrecioVenta",
                "El precio de venta no puede ser negativo.");
        }

        if (Input.PrecioCompra > 0 && Input.PrecioVenta < Input.PrecioCompra)
        {
            ModelState.AddModelError(
                "Input.PrecioVenta",
                "El precio de venta no puede ser menor que el precio de compra.");
        }

        if (Input.ControlaStock && Input.StockMinimo < 0)
        {
            ModelState.AddModelError(
                "Input.StockMinimo",
                "El stock mínimo no puede ser negativo.");
        }

        if (Input.ControlaStock &&
            Input.StockMaximo.HasValue &&
            Input.StockMaximo.Value < 0)
        {
            ModelState.AddModelError(
                "Input.StockMaximo",
                "El stock máximo no puede ser negativo.");
        }

        if (Input.ControlaStock &&
            Input.StockMaximo.HasValue &&
            Input.StockMaximo.Value < Input.StockMinimo)
        {
            ModelState.AddModelError(
                "Input.StockMaximo",
                "El stock máximo debe ser mayor o igual al stock mínimo.");
        }
    }


    // =========================================================
    // REQUEST.FORM
    // =========================================================

    private static string LeerTexto(
        IFormCollection form,
        string clave)
    {
        return form.TryGetValue(clave, out StringValues valor)
            ? valor.ToString().Trim()
            : string.Empty;
    }

    private static string? LeerTextoNullable(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor;
    }

    private static int LeerInt(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        return int.TryParse(valor, out int resultado)
            ? resultado
            : 0;
    }

    private static int? LeerIntNullable(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return int.TryParse(valor, out int resultado)
            ? resultado
            : null;
    }

    private static decimal LeerDecimal(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out decimal actual))
        {
            return actual;
        }

        return 0M;
    }

    private static decimal? LeerDecimalNullable(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out decimal actual))
        {
            return actual;
        }

        return null;
    }

    private static bool LeerBool(
        IFormCollection form,
        string clave)
    {
        if (!form.TryGetValue(clave, out StringValues valores))
        {
            return false;
        }

        foreach (string? valor in valores)
        {
            if (bool.TryParse(valor, out bool resultado) && resultado)
            {
                return true;
            }

            if (string.Equals(valor, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // CATÁLOGOS
    // =========================================================

    private async Task CargarCatalogosAsync(
        CancellationToken cancellationToken)
    {
        Catalogos =
            await _productoService.ObtenerCatalogosAsync(
                IdEmpresa,
                cancellationToken);
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    private bool CargarContextoSesion()
    {
        IdUsuario = ObtenerEnteroSesion(SessionKeys.IdUsuario);
        IdEmpresa = ObtenerEnteroSesion("IdEmpresa");
        IdSucursal = ObtenerEnteroSesion("IdSucursal");

        if (IdUsuario <= 0)
        {
            return false;
        }

        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }

        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }

        return true;
    }

    private int ObtenerEnteroSesion(string clave)
    {
        int? entero = HttpContext.Session.GetInt32(clave);

        if (entero.HasValue)
        {
            return entero.Value;
        }

        string? texto = HttpContext.Session.GetString(clave);

        return int.TryParse(texto, out int resultado)
            ? resultado
            : 0;
    }


    // =========================================================
    // LOG
    // =========================================================

    private void LogModelState()
    {
        foreach (KeyValuePair<string, ModelStateEntry?> item in ModelState)
        {
            if (item.Value == null || item.Value.Errors.Count == 0)
            {
                continue;
            }

            foreach (ModelError error in item.Value.Errors)
            {
                string mensaje =
                    !string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.ErrorMessage
                        : error.Exception?.Message ?? "Valor inválido.";

                _logger.LogWarning(
                    "[PRODUCTO NUEVO] ModelState inválido. Campo={Campo} Error={Error}",
                    item.Key,
                    mensaje);
            }
        }
    }


    private static ProductoGuardarRequest CrearInputInicial()
    {
        return new ProductoGuardarRequest
        {
            ControlaStock = true,
            StockMinimo = 0M,
            StockMaximo = null,
            PrecioCompra = 0M,
            PrecioVenta = 0M
        };
    }
}
