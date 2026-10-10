using KerniX.Models.Configuracion.Usuarios;

namespace KerniX.Services.Configuracion.Usuarios;

public interface IUsuarioAdminDbService
{
    Task<UsuarioAdminResultado> ListarAsync(
        UsuarioAdminFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<UsuarioAdminResumen> ObtenerResumenAsync(
        int idEmpresa,
        int? idSucursal,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolAdminItem>> ListarRolesAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<EmpleadoUsuarioDisponibleItem>>
        ListarEmpleadosDisponiblesAsync(
            int idEmpresa,
            int? idSucursal,
            CancellationToken cancellationToken = default);

    Task<int> CrearUsuarioAsync(
        UsuarioCrearAdminRequest request,
        CancellationToken cancellationToken = default);


    Task<UsuarioAdminEditarDetalle?> ObtenerParaEditarAsync(
        int idUsuario,
        int idEmpresa,
        CancellationToken cancellationToken = default);

    Task ActualizarUsuarioAsync(
        UsuarioAdminActualizarRequest request,
        CancellationToken cancellationToken = default);

    Task RestablecerClaveAsync(
        int idUsuario,
        string passwordTemporal,
        bool requiereCambioClave,
        CancellationToken cancellationToken = default);


    Task<UsuarioMonitoreoResultado> ObtenerMonitoreoAsync(
        int idUsuario,
        int idEmpresa,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoAsync(
        int idUsuario,
        bool estado,
        int idUsuarioModifica,
        CancellationToken cancellationToken = default);

    Task BloquearAsync(
        int idUsuario,
        int minutos,
        CancellationToken cancellationToken = default);

    Task DesbloquearAsync(
        int idUsuario,
        CancellationToken cancellationToken = default);

    Task RevocarSesionesAsync(
        int idUsuario,
        CancellationToken cancellationToken = default);

    Task AsignarRolAsync(
        int idUsuario,
        int idRol,
        int idUsuarioRegistro,
        CancellationToken cancellationToken = default);

    Task QuitarRolAsync(
        int idUsuario,
        int idRol,
        CancellationToken cancellationToken = default);
}
