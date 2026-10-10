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

public sealed class EditarModel : PageModel
{
    private readonly IProductoDbService _productoService;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(
        IProductoDbService productoService,
        ILogger<EditarModel> logger)
    {
        _productoService = productoService;
        _logger = logger;
    }


    // =========================================================
    // FORMULARIO
    // =========================================================

    [BindProperty]
    public ProductoActualizarRequest Input { get; set; } =
        CrearInputInicial();

    public ProductoCatalogos Catalogos { get; private set; } =
        new();

    public ProductoDetalle? ProductoOriginal { get; private set; }

    public int IdProducto { get; private set; }

    public int IdUsuario { get; private set; }

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }

    public string? MensajeError { get; private set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        int idProducto,
        CancellationToken cancellationToken)
    {
        if (idProducto <= 0)
        {
            return BadRequest(
                "El identificador del producto no es válido."
            );
        }

        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        IdProducto = idProducto;

        try
        {
            await CargarCatalogosAsync(
                cancellationToken
            );

            ProductoOriginal =
                await _productoService
                    .ObtenerAsync(
                        idProducto,
                        cancellationToken
                    );

            if (ProductoOriginal == null)
            {
                return NotFound(
                    "No se encontró el producto solicitado."
                );
            }

            if (
                ProductoOriginal.IdEmpresa > 0 &&
                ProductoOriginal.IdEmpresa != IdEmpresa
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "El producto no pertenece a la empresa seleccionada."
                );
            }

            // En Detalle ya vienen los IDs reales:
            // IdCategoriaProducto, IdMarca e IdUnidadMedida.
            Input =
                new ProductoActualizarRequest
                {
                    IdProducto =
                        ProductoOriginal.IdProducto,

                    IdEmpresa =
                        IdEmpresa,

                    IdCategoriaProducto =
                        ProductoOriginal.IdCategoriaProducto,

                    IdMarca =
                        ProductoOriginal.IdMarca,

                    IdUnidadMedida =
                        ProductoOriginal.IdUnidadMedida,

                    Codigo =
                        ProductoOriginal.Codigo
                        ?? string.Empty,

                    CodigoBarras =
                        ProductoOriginal.CodigoBarras,

                    Nombre =
                        ProductoOriginal.Nombre
                        ?? string.Empty,

                    Descripcion =
                        ProductoOriginal.Descripcion,

                    PrecioCompra =
                        ProductoOriginal.PrecioCompra,

                    PrecioVenta =
                        ProductoOriginal.PrecioVenta,

                    StockMinimo =
                        ProductoOriginal.StockMinimo,

                    StockMaximo =
                        ProductoOriginal.StockMaximo,

                    ControlaStock =
                        ProductoOriginal.ControlaStock,

                    Estado =
                        ProductoOriginal.Estado,

                    IdUsuario =
                        IdUsuario
                };

            _logger.LogInformation(
                "[PRODUCTO EDITAR] GET. Producto={Producto} Usuario={Usuario} Empresa={Empresa} Categoria={Categoria} Marca={Marca} Unidad={Unidad}",
                IdProducto,
                IdUsuario,
                IdEmpresa,
                Input.IdCategoriaProducto,
                Input.IdMarca,
                Input.IdUnidadMedida
            );

            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTO EDITAR] ERROR SQL cargando Producto={Producto}. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea}",
                IdProducto,
                ex.Number,
                ex.Procedure,
                ex.LineNumber
            );

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
                "[PRODUCTO EDITAR] Error cargando Producto={Producto}.",
                IdProducto
            );

            MensajeError =
                ex.Message;

            return Page();
        }
    }


    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostAsync(
        int idProducto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[PRODUCTO EDITAR] POST recibido. Producto={Producto}",
            idProducto
        );

        if (idProducto <= 0)
        {
            return BadRequest(
                "El identificador del producto no es válido."
            );
        }

        if (!CargarContextoSesion())
        {
            _logger.LogWarning(
                "[PRODUCTO EDITAR] POST sin sesión válida."
            );

            return RedirectToPage(
                "/Account/Login"
            );
        }

        IdProducto = idProducto;

        try
        {
            // =================================================
            // PRODUCTO ORIGINAL
            // =================================================
            // Se obtiene antes del Request para conservar Estado
            // y validar que pertenece a la empresa de la sesión.

            ProductoOriginal =
                await _productoService
                    .ObtenerAsync(
                        IdProducto,
                        cancellationToken
                    );

            if (ProductoOriginal == null)
            {
                return NotFound(
                    "No se encontró el producto solicitado."
                );
            }

            if (
                ProductoOriginal.IdEmpresa > 0 &&
                ProductoOriginal.IdEmpresa != IdEmpresa
            )
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "El producto no pertenece a la empresa seleccionada."
                );
            }


            // =================================================
            // REQUEST.FORM MANUAL
            // =================================================
            // MISMO PROCEDIMIENTO QUE NUEVO PRODUCTO.
            // No dependemos del model binder.

            if (!Request.HasFormContentType)
            {
                throw new InvalidOperationException(
                    "La solicitud no contiene datos de formulario."
                );
            }

            IFormCollection form =
                await Request.ReadFormAsync(
                    cancellationToken
                );

            _logger.LogInformation(
                "[PRODUCTO EDITAR] Campos recibidos: {Campos}",
                string.Join(
                    ", ",
                    form.Keys
                )
            );


            // =================================================
            // CONSTRUIR ProductoActualizarRequest DIRECTAMENTE
            // =================================================

            Input =
                new ProductoActualizarRequest
                {
                    IdProducto =
                        IdProducto,

                    IdEmpresa =
                        IdEmpresa,

                    IdUsuario =
                        IdUsuario,

                    Estado =
                        ProductoOriginal.Estado,

                    Codigo =
                        LeerTexto(
                            form,
                            "Input.Codigo"
                        ),

                    CodigoBarras =
                        LeerTextoNullable(
                            form,
                            "Input.CodigoBarras"
                        ),

                    Nombre =
                        LeerTexto(
                            form,
                            "Input.Nombre"
                        ),

                    Descripcion =
                        LeerTextoNullable(
                            form,
                            "Input.Descripcion"
                        ),

                    IdCategoriaProducto =
                        LeerInt(
                            form,
                            "Input.IdCategoriaProducto"
                        ),

                    IdMarca =
                        LeerIntNullable(
                            form,
                            "Input.IdMarca"
                        ),

                    IdUnidadMedida =
                        LeerInt(
                            form,
                            "Input.IdUnidadMedida"
                        ),

                    PrecioCompra =
                        LeerDecimal(
                            form,
                            "Input.PrecioCompra"
                        ),

                    PrecioVenta =
                        LeerDecimal(
                            form,
                            "Input.PrecioVenta"
                        ),

                    StockMinimo =
                        LeerDecimal(
                            form,
                            "Input.StockMinimo"
                        ),

                    StockMaximo =
                        LeerDecimalNullable(
                            form,
                            "Input.StockMaximo"
                        ),

                    ControlaStock =
                        LeerBool(
                            form,
                            "Input.ControlaStock"
                        )
                };


            if (!Input.ControlaStock)
            {
                Input.StockMinimo = 0M;
                Input.StockMaximo = null;
            }


            // =================================================
            // LIMPIAR MODELSTATE DEL BINDER
            // =================================================
            // Igual que en Nuevo: el binder pudo agregar errores
            // antes de que leyéramos Request.Form manualmente.

            ModelState.Clear();


            // =================================================
            // LOG DATOS
            // =================================================

            _logger.LogInformation(
                "[PRODUCTO EDITAR] Datos POST. Producto={Producto} Usuario={Usuario} Empresa={Empresa} Categoria={Categoria} Marca={Marca} Unidad={Unidad} Codigo={Codigo} Nombre={Nombre} Compra={Compra} Venta={Venta} Minimo={Minimo} Maximo={Maximo} ControlStock={ControlStock} Estado={Estado}",
                Input.IdProducto,
                Input.IdUsuario,
                Input.IdEmpresa,
                Input.IdCategoriaProducto,
                Input.IdMarca,
                Input.IdUnidadMedida,
                Input.Codigo,
                Input.Nombre,
                Input.PrecioCompra,
                Input.PrecioVenta,
                Input.StockMinimo,
                Input.StockMaximo,
                Input.ControlaStock,
                Input.Estado
            );


            // =================================================
            // VALIDAR
            // =================================================

            ValidarFormulario();


            if (!ModelState.IsValid)
            {
                LogModelState();

                await CargarCatalogosAsync(
                    cancellationToken
                );

                MensajeError =
                    "Existen datos inválidos. Revisa los mensajes mostrados en el formulario.";

                return Page();
            }


            // =================================================
            // ACTUALIZAR DIRECTAMENTE
            // =================================================
            // SIN reflexión.
            // SIN ProductoGuardarRequest.
            // El servicio real recibe ProductoActualizarRequest.

            _logger.LogInformation(
                "[PRODUCTO EDITAR] Ejecutando ActualizarAsync. Producto={Producto} Codigo={Codigo}",
                Input.IdProducto,
                Input.Codigo
            );

            await _productoService
                .ActualizarAsync(
                    Input,
                    cancellationToken
                );


            _logger.LogInformation(
                "[PRODUCTO EDITAR] Producto actualizado correctamente. Producto={Producto} Codigo={Codigo} Usuario={Usuario} Empresa={Empresa}",
                Input.IdProducto,
                Input.Codigo,
                IdUsuario,
                IdEmpresa
            );


            TempData["ProductoExito"] =
                "Producto actualizado correctamente.";


            return RedirectToPage(
                "./Detalle",
                new
                {
                    idProducto =
                        Input.IdProducto
                }
            );
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTO EDITAR] ERROR SQL. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea} Mensaje={Mensaje} Producto={Producto} Usuario={Usuario} Empresa={Empresa}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber,
                ex.Message,
                IdProducto,
                IdUsuario,
                IdEmpresa
            );

            await CargarCatalogosSeguroAsync(
                cancellationToken
            );

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
                "[PRODUCTO EDITAR] Error general actualizando Producto={Producto}.",
                IdProducto
            );

            await CargarCatalogosSeguroAsync(
                cancellationToken
            );

            MensajeError =
                ex.Message;

            return Page();
        }
    }


    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private void ValidarFormulario()
    {
        if (Input.IdProducto <= 0)
        {
            ModelState.AddModelError(
                "Input.IdProducto",
                "El producto es obligatorio."
            );
        }

        if (Input.IdEmpresa <= 0)
        {
            ModelState.AddModelError(
                "Input.IdEmpresa",
                "La empresa es obligatoria."
            );
        }

        if (Input.IdUsuario <= 0)
        {
            ModelState.AddModelError(
                "Input.IdUsuario",
                "No se pudo identificar al usuario."
            );
        }

        if (Input.IdCategoriaProducto <= 0)
        {
            ModelState.AddModelError(
                "Input.IdCategoriaProducto",
                "Selecciona una categoría."
            );
        }

        if (Input.IdUnidadMedida <= 0)
        {
            ModelState.AddModelError(
                "Input.IdUnidadMedida",
                "Selecciona una unidad de medida."
            );
        }

        if (string.IsNullOrWhiteSpace(Input.Codigo))
        {
            ModelState.AddModelError(
                "Input.Codigo",
                "El código del producto es obligatorio."
            );
        }
        else if (Input.Codigo.Length > 100)
        {
            ModelState.AddModelError(
                "Input.Codigo",
                "El código no puede superar los 100 caracteres."
            );
        }

        if (Input.CodigoBarras?.Length > 100)
        {
            ModelState.AddModelError(
                "Input.CodigoBarras",
                "El código de barras no puede superar los 100 caracteres."
            );
        }

        if (string.IsNullOrWhiteSpace(Input.Nombre))
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "El nombre del producto es obligatorio."
            );
        }
        else if (Input.Nombre.Length > 200)
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "El nombre no puede superar los 200 caracteres."
            );
        }

        if (Input.Descripcion?.Length > 500)
        {
            ModelState.AddModelError(
                "Input.Descripcion",
                "La descripción no puede superar los 500 caracteres."
            );
        }

        if (Input.PrecioCompra < 0)
        {
            ModelState.AddModelError(
                "Input.PrecioCompra",
                "El precio de compra no puede ser negativo."
            );
        }

        if (Input.PrecioVenta < 0)
        {
            ModelState.AddModelError(
                "Input.PrecioVenta",
                "El precio de venta no puede ser negativo."
            );
        }

        if (
            Input.PrecioCompra > 0 &&
            Input.PrecioVenta <
            Input.PrecioCompra
        )
        {
            ModelState.AddModelError(
                "Input.PrecioVenta",
                "El precio de venta no puede ser menor que el precio de compra."
            );
        }

        if (
            Input.ControlaStock &&
            Input.StockMinimo < 0
        )
        {
            ModelState.AddModelError(
                "Input.StockMinimo",
                "El stock mínimo no puede ser negativo."
            );
        }

        if (
            Input.ControlaStock &&
            Input.StockMaximo.HasValue &&
            Input.StockMaximo.Value < 0
        )
        {
            ModelState.AddModelError(
                "Input.StockMaximo",
                "El stock máximo no puede ser negativo."
            );
        }

        if (
            Input.ControlaStock &&
            Input.StockMaximo.HasValue &&
            Input.StockMaximo.Value <
            Input.StockMinimo
        )
        {
            ModelState.AddModelError(
                "Input.StockMaximo",
                "El stock máximo debe ser mayor o igual al stock mínimo."
            );
        }
    }


    // =========================================================
    // REQUEST FORM HELPERS
    // =========================================================

    private static string LeerTexto(
        IFormCollection form,
        string clave)
    {
        return form.TryGetValue(
            clave,
            out StringValues valor
        )
            ? valor.ToString().Trim()
            : string.Empty;
    }

    private static string? LeerTextoNullable(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );

        return string.IsNullOrWhiteSpace(
            valor
        )
            ? null
            : valor;
    }

    private static int LeerInt(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );

        return int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int resultado
        )
            ? resultado
            : 0;
    }

    private static int? LeerIntNullable(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );

        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int resultado
        )
            ? resultado
            : null;
    }

    private static decimal LeerDecimal(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );

        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal invariant
            )
        )
        {
            return invariant;
        }

        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal actual
            )
        )
        {
            return actual;
        }

        return 0M;
    }

    private static decimal? LeerDecimalNullable(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );

        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal invariant
            )
        )
        {
            return invariant;
        }

        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal actual
            )
        )
        {
            return actual;
        }

        return null;
    }

    private static bool LeerBool(
        IFormCollection form,
        string clave)
    {
        if (
            !form.TryGetValue(
                clave,
                out StringValues valores
            )
        )
        {
            return false;
        }

        foreach (string? valor in valores)
        {
            if (
                bool.TryParse(
                    valor,
                    out bool resultado
                )
                &&
                resultado
            )
            {
                return true;
            }

            if (
                string.Equals(
                    valor,
                    "1",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    valor,
                    "on",
                    StringComparison.OrdinalIgnoreCase
                )
            )
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
            await _productoService
                .ObtenerCatalogosAsync(
                    IdEmpresa,
                    cancellationToken
                );
    }

    private async Task CargarCatalogosSeguroAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await CargarCatalogosAsync(
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[PRODUCTO EDITAR] No se pudieron recargar los catálogos después del error."
            );
        }
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    private bool CargarContextoSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );

        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );

        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );

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

    private int ObtenerEnteroSesion(
        string clave)
    {
        int? entero =
            HttpContext.Session.GetInt32(
                clave
            );

        if (entero.HasValue)
        {
            return entero.Value;
        }

        string? texto =
            HttpContext.Session.GetString(
                clave
            );

        return int.TryParse(
            texto,
            out int resultado
        )
            ? resultado
            : 0;
    }


    // =========================================================
    // LOG MODELSTATE
    // =========================================================

    private void LogModelState()
    {
        foreach (
            KeyValuePair<string, ModelStateEntry?> item
            in ModelState
        )
        {
            if (
                item.Value == null ||
                item.Value.Errors.Count == 0
            )
            {
                continue;
            }

            foreach (
                ModelError error
                in item.Value.Errors
            )
            {
                string mensaje =
                    !string.IsNullOrWhiteSpace(
                        error.ErrorMessage
                    )
                        ? error.ErrorMessage
                        : error.Exception?.Message
                          ?? "Valor inválido.";

                _logger.LogWarning(
                    "[PRODUCTO EDITAR] ModelState inválido. Campo={Campo} Error={Error}",
                    item.Key,
                    mensaje
                );
            }
        }
    }


    // =========================================================
    // INPUT INICIAL
    // =========================================================

    private static ProductoActualizarRequest CrearInputInicial()
    {
        return new ProductoActualizarRequest
        {
            ControlaStock = true,
            StockMinimo = 0M,
            StockMaximo = null,
            PrecioCompra = 0M,
            PrecioVenta = 0M,
            Estado = true
        };
    }


    // =========================================================
    // HTML DECIMAL
    // =========================================================

    public string DecimalHtml(
        decimal valor)
    {
        return valor.ToString(
            "0.####",
            CultureInfo.InvariantCulture
        );
    }

    public string DecimalNullableHtml(
        decimal? valor)
    {
        return valor.HasValue
            ? valor.Value.ToString(
                "0.####",
                CultureInfo.InvariantCulture
            )
            : string.Empty;
    }
}

