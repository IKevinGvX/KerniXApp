using KerniX.Models.Usuarios;

namespace KerniX.Services.Usuarios;

public interface IUsuarioService
{
    Task<UsuarioPerfilResponse?> ObtenerPerfilAsync(
        int idUsuario);

    Task<ApiOperationResult> ActualizarPerfilAsync(
        int idUsuario,
        ActualizarUsuarioRequest request);

    Task<ApiOperationResult> CambiarPasswordAsync(
        int idUsuario,
        CambiarPasswordRequest request);
}