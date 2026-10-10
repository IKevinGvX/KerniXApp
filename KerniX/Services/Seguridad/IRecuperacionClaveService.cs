using System.Threading;
using System.Threading.Tasks;

namespace KerniX.Services.Seguridad;

public interface IRecuperacionClaveService
{
    Task SolicitarAsync(string correo, string ip, CancellationToken cancellationToken);
    Task<bool> TokenEsValidoAsync(string token, CancellationToken cancellationToken);
    Task<bool> RestablecerAsync(string token, string nuevaClave, CancellationToken cancellationToken);
}
