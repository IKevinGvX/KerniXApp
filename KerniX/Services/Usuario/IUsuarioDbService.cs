using KerniX.Models.Usuarios;

namespace KerniX.Services.Usuarios;

public interface IUsuarioDbService
{
    Task<UsuarioPerfilResponse?>
        ObtenerPerfilAsync(
            int idUsuario);

    Task ActualizarPerfilAsync(
        int idUsuario,
        string nombreUsuario,
        string correo);
}