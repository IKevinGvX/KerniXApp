using KerniX.Models.Auth;

namespace KerniX.Services.Empleados;

public interface IEmpleadoDbService
{
    Task<UsuarioEmpleadoDb?> ObtenerPorUsuarioAsync(
        string usuario,
        CancellationToken cancellationToken = default);
}