using KerniX.Models.IA;

namespace KerniX.Services.IA;

public interface IConversacionIaService
{
    Task<ResultadoConversacionIa> ConversarAsync(
        string mensaje, CancellationToken cancellationToken = default);
}
