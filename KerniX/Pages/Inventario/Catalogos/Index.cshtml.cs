using KerniX.Models.Inventario.Catalogos;
using KerniX.Security;
using KerniX.Services.Inventario.Catalogos;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Catalogos;

public sealed class IndexModel : PageModel
{
    private readonly ICatalogoInventarioDbService _catalogoService;
    private readonly ILogger<IndexModel> _logger;


    public IndexModel(
        ICatalogoInventarioDbService catalogoService,
        ILogger<IndexModel> logger)
    {
        _catalogoService =
            catalogoService;

        _logger =
            logger;
    }


    public int IdUsuario { get; private set; }
    public int IdEmpresa { get; private set; }


    [BindProperty(SupportsGet = true)]
    public string? BuscarCategoria { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EstadoCategoria { get; set; }


    [BindProperty(SupportsGet = true)]
    public string? BuscarUnidad { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EstadoUnidad { get; set; }


    [BindProperty(SupportsGet = true)]
    public string? BuscarMarca { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EstadoMarca { get; set; }


    public IReadOnlyList<CategoriaProductoAdminItem>
        Categorias
    { get; private set; } =
        Array.Empty<CategoriaProductoAdminItem>();


    public IReadOnlyList<UnidadMedidaAdminItem>
        Unidades
    { get; private set; } =
        Array.Empty<UnidadMedidaAdminItem>();


    public IReadOnlyList<MarcaAdminItem>
        Marcas
    { get; private set; } =
        Array.Empty<MarcaAdminItem>();


    [TempData]
    public string? Mensaje { get; set; }

    [TempData]
    public string? Error { get; set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        await CargarDatosAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "[CATALOGOS INVENTARIO] GET. Usuario={Usuario} Empresa={Empresa} Categorias={Categorias} Unidades={Unidades} Marcas={Marcas}",
            IdUsuario,
            IdEmpresa,
            Categorias.Count,
            Unidades.Count,
            Marcas.Count
        );


        return Page();
    }


    // =========================================================
    // CREAR CATEGORÍA
    // =========================================================

    public async Task<IActionResult> OnPostCrearCategoriaAsync(
        string nombre,
        string? descripcion,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            int id =
                await _catalogoService
                    .CrearCategoriaAsync(
                        new CategoriaProductoGuardarRequest
                        {
                            IdEmpresa =
                                IdEmpresa,

                            Nombre =
                                nombre,

                            Descripcion =
                                descripcion,

                            Estado =
                                true
                        },
                        cancellationToken
                    );


            Mensaje =
                $"Categoría registrada correctamente. ID {id}.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "crear categoría"
            );
        }


        return Volver();
    }


    // =========================================================
    // EDITAR CATEGORÍA
    // =========================================================

    public async Task<IActionResult> OnPostEditarCategoriaAsync(
        int idCategoriaProducto,
        string nombre,
        string? descripcion,
        bool estado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .ActualizarCategoriaAsync(
                    new CategoriaProductoActualizarRequest
                    {
                        IdCategoriaProducto =
                            idCategoriaProducto,

                        IdEmpresa =
                            IdEmpresa,

                        Nombre =
                            nombre,

                        Descripcion =
                            descripcion,

                        Estado =
                            estado
                    },
                    cancellationToken
                );


            Mensaje =
                "Categoría actualizada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "editar categoría"
            );
        }


        return Volver();
    }


    // =========================================================
    // ELIMINAR LÓGICO / ACTIVAR CATEGORÍA
    // =========================================================

    public async Task<IActionResult> OnPostEstadoCategoriaAsync(
        int idCategoriaProducto,
        bool nuevoEstado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .CambiarEstadoCategoriaAsync(
                    idCategoriaProducto,
                    IdEmpresa,
                    nuevoEstado,
                    cancellationToken
                );


            Mensaje =
                nuevoEstado
                    ? "Categoría activada correctamente."
                    : "Categoría desactivada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "cambiar estado de categoría"
            );
        }


        return Volver();
    }


    // =========================================================
    // CREAR UNIDAD
    // =========================================================

    public async Task<IActionResult> OnPostCrearUnidadAsync(
        string codigo,
        string nombre,
        string abreviatura,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            int id =
                await _catalogoService
                    .CrearUnidadAsync(
                        new UnidadMedidaGuardarRequest
                        {
                            Codigo =
                                codigo,

                            Nombre =
                                nombre,

                            Abreviatura =
                                abreviatura,

                            Estado =
                                true
                        },
                        cancellationToken
                    );


            Mensaje =
                $"Unidad de medida registrada correctamente. ID {id}.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "crear unidad de medida"
            );
        }


        return Volver();
    }


    // =========================================================
    // EDITAR UNIDAD
    // =========================================================

    public async Task<IActionResult> OnPostEditarUnidadAsync(
        int idUnidadMedida,
        string codigo,
        string nombre,
        string abreviatura,
        bool estado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .ActualizarUnidadAsync(
                    new UnidadMedidaActualizarRequest
                    {
                        IdUnidadMedida =
                            idUnidadMedida,

                        Codigo =
                            codigo,

                        Nombre =
                            nombre,

                        Abreviatura =
                            abreviatura,

                        Estado =
                            estado
                    },
                    cancellationToken
                );


            Mensaje =
                "Unidad de medida actualizada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "editar unidad de medida"
            );
        }


        return Volver();
    }


    // =========================================================
    // ELIMINAR LÓGICO / ACTIVAR UNIDAD
    // =========================================================

    public async Task<IActionResult> OnPostEstadoUnidadAsync(
        int idUnidadMedida,
        bool nuevoEstado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .CambiarEstadoUnidadAsync(
                    idUnidadMedida,
                    nuevoEstado,
                    cancellationToken
                );


            Mensaje =
                nuevoEstado
                    ? "Unidad de medida activada correctamente."
                    : "Unidad de medida desactivada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "cambiar estado de unidad"
            );
        }


        return Volver();
    }


    // =========================================================
    // CREAR MARCA
    // =========================================================

    public async Task<IActionResult> OnPostCrearMarcaAsync(
        string nombre,
        string? descripcion,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            int id =
                await _catalogoService
                    .CrearMarcaAsync(
                        new MarcaGuardarRequest
                        {
                            IdEmpresa =
                                IdEmpresa,

                            Nombre =
                                nombre,

                            Descripcion =
                                descripcion,

                            Estado =
                                true
                        },
                        cancellationToken
                    );


            Mensaje =
                $"Marca registrada correctamente. ID {id}.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "crear marca"
            );
        }


        return Volver();
    }


    // =========================================================
    // EDITAR MARCA
    // =========================================================

    public async Task<IActionResult> OnPostEditarMarcaAsync(
        int idMarca,
        string nombre,
        string? descripcion,
        bool estado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .ActualizarMarcaAsync(
                    new MarcaActualizarRequest
                    {
                        IdMarca =
                            idMarca,

                        IdEmpresa =
                            IdEmpresa,

                        Nombre =
                            nombre,

                        Descripcion =
                            descripcion,

                        Estado =
                            estado
                    },
                    cancellationToken
                );


            Mensaje =
                "Marca actualizada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "editar marca"
            );
        }


        return Volver();
    }


    // =========================================================
    // ELIMINAR LÓGICO / ACTIVAR MARCA
    // =========================================================

    public async Task<IActionResult> OnPostEstadoMarcaAsync(
        int idMarca,
        bool nuevoEstado,
        CancellationToken cancellationToken)
    {
        if (!CargarSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await _catalogoService
                .CambiarEstadoMarcaAsync(
                    idMarca,
                    IdEmpresa,
                    nuevoEstado,
                    cancellationToken
                );


            Mensaje =
                nuevoEstado
                    ? "Marca activada correctamente."
                    : "Marca desactivada correctamente.";
        }
        catch (Exception ex)
        {
            ProcesarError(
                ex,
                "cambiar estado de marca"
            );
        }


        return Volver();
    }


    // =========================================================
    // CARGA
    // =========================================================

    private async Task CargarDatosAsync(
        CancellationToken cancellationToken)
    {
        bool? estadoCategoria =
            ParseEstado(
                EstadoCategoria
            );

        bool? estadoUnidad =
            ParseEstado(
                EstadoUnidad
            );

        bool? estadoMarca =
            ParseEstado(
                EstadoMarca
            );


        Task<IReadOnlyList<CategoriaProductoAdminItem>>
            categoriasTask =
                _catalogoService
                    .ListarCategoriasAsync(
                        IdEmpresa,
                        BuscarCategoria,
                        estadoCategoria,
                        cancellationToken
                    );


        Task<IReadOnlyList<UnidadMedidaAdminItem>>
            unidadesTask =
                _catalogoService
                    .ListarUnidadesAsync(
                        BuscarUnidad,
                        estadoUnidad,
                        cancellationToken
                    );


        Task<IReadOnlyList<MarcaAdminItem>>
            marcasTask =
                _catalogoService
                    .ListarMarcasAsync(
                        IdEmpresa,
                        BuscarMarca,
                        estadoMarca,
                        cancellationToken
                    );


        await Task.WhenAll(
            categoriasTask,
            unidadesTask,
            marcasTask
        );


        Categorias =
            await categoriasTask;

        Unidades =
            await unidadesTask;

        Marcas =
            await marcasTask;
    }


    private bool CargarSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );


        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );


        if (IdUsuario <= 0)
        {
            return false;
        }


        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        return true;
    }


    private int ObtenerEnteroSesion(
        string clave)
    {
        int? valor =
            HttpContext.Session.GetInt32(
                clave
            );


        if (valor.HasValue)
        {
            return valor.Value;
        }


        string? texto =
            HttpContext.Session.GetString(
                clave
            );


        return int.TryParse(
            texto,
            out int numero
        )
            ? numero
            : 0;
    }


    private static bool? ParseEstado(
        string? valor)
    {
        return valor switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
    }


    private IActionResult Volver()
    {
        return RedirectToPage(
            "./Index",
            new
            {
                BuscarCategoria =
                    string.IsNullOrWhiteSpace(
                        BuscarCategoria
                    )
                        ? null
                        : BuscarCategoria,

                EstadoCategoria =
                    string.IsNullOrWhiteSpace(
                        EstadoCategoria
                    )
                        ? null
                        : EstadoCategoria,

                BuscarUnidad =
                    string.IsNullOrWhiteSpace(
                        BuscarUnidad
                    )
                        ? null
                        : BuscarUnidad,

                EstadoUnidad =
                    string.IsNullOrWhiteSpace(
                        EstadoUnidad
                    )
                        ? null
                        : EstadoUnidad,

                BuscarMarca =
                    string.IsNullOrWhiteSpace(
                        BuscarMarca
                    )
                        ? null
                        : BuscarMarca,

                EstadoMarca =
                    string.IsNullOrWhiteSpace(
                        EstadoMarca
                    )
                        ? null
                        : EstadoMarca
            }
        );
    }


    private void ProcesarError(
        Exception ex,
        string operacion)
    {
        _logger.LogError(
            ex,
            "[CATALOGOS INVENTARIO] Error al {Operacion}.",
            operacion
        );


        Error =
            ex switch
            {
                SqlException sqlEx
                    when sqlEx.Number is
                        65001 or
                        65002 or
                        65003 or
                        65004 or
                        65101 or
                        65102 or
                        65103 or
                        65104 or
                        65201 or
                        65202 or
                        65203 or
                        65204
                    =>
                        sqlEx.Message,

                SqlException sqlEx
                    when sqlEx.Number is
                        2601 or
                        2627
                    =>
                        "Ya existe un registro con esos datos.",

                ArgumentException argEx
                    =>
                        argEx.Message,

                _ =>
                    "No fue posible completar la operación."
            };
    }
}
