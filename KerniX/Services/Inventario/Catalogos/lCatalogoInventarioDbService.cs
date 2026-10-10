using KerniX.Models.Inventario.Catalogos;

namespace KerniX.Services.Inventario.Catalogos;

public interface ICatalogoInventarioDbService
{
    // =========================================================
    // CATEGORÍAS
    // =========================================================

    Task<IReadOnlyList<CategoriaProductoAdminItem>>
        ListarCategoriasAsync(
            int idEmpresa,
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default);

    Task<int> CrearCategoriaAsync(
        CategoriaProductoGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task ActualizarCategoriaAsync(
        CategoriaProductoActualizarRequest request,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoCategoriaAsync(
        int idCategoriaProducto,
        int idEmpresa,
        bool estado,
        CancellationToken cancellationToken = default);


    // =========================================================
    // UNIDADES DE MEDIDA
    // =========================================================

    Task<IReadOnlyList<UnidadMedidaAdminItem>>
        ListarUnidadesAsync(
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default);

    Task<int> CrearUnidadAsync(
        UnidadMedidaGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task ActualizarUnidadAsync(
        UnidadMedidaActualizarRequest request,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoUnidadAsync(
        int idUnidadMedida,
        bool estado,
        CancellationToken cancellationToken = default);


    // =========================================================
    // MARCAS
    // =========================================================

    Task<IReadOnlyList<MarcaAdminItem>>
        ListarMarcasAsync(
            int idEmpresa,
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default);

    Task<int> CrearMarcaAsync(
        MarcaGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task ActualizarMarcaAsync(
        MarcaActualizarRequest request,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoMarcaAsync(
        int idMarca,
        int idEmpresa,
        bool estado,
        CancellationToken cancellationToken = default);
}
