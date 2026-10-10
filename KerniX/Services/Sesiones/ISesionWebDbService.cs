using Microsoft.AspNetCore.Http;

namespace KerniX.Services.Sesiones;

public sealed class SesionWebResumen
{
    public Guid IdSesion { get; init; }
    public DateTime FechaInicioUtc { get; init; }
    public DateTime UltimoAccesoUtc { get; init; }
    public string Dispositivo { get; init; } = "";
    public string Navegador { get; init; } = "";
    public string? Ip { get; init; }
}

public interface ISesionWebDbService
{
    Task RegistrarAsync(Guid idSesion, int idUsuario,
        HttpContext context, CancellationToken cancellationToken);

    Task<bool> ValidarYActualizarAsync(Guid idSesion, int idUsuario,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SesionWebResumen>> ListarActivasAsync(
        int idUsuario, CancellationToken cancellationToken);

    Task<bool> CerrarAsync(Guid idSesion, int idUsuario,
        string motivo, CancellationToken cancellationToken);

    Task<int> CerrarOtrasAsync(Guid idSesionActual, int idUsuario,
        CancellationToken cancellationToken);
}
