using KerniX.Models.IA;

namespace KerniX.Services.IA;

public interface IAgenteCatalogoService
{
    // Mantiene el método de Fase 2/Fase 3 (no modifica llamadas existentes).
    Task<IReadOnlyList<AgenteCatalogoProducto>> BuscarAsync(
        int idEmpresa,
        string consulta,
        CancellationToken cancellationToken = default);

    // Fase 5: recuperar por ID para revalidar precio/stock con empresa verificada.
    // No se aceptan precios ni descripciones enviados por el navegador.
    Task<IReadOnlyList<AgenteCatalogoProducto>> ObtenerPorIdsAsync(
        int idEmpresa,
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);

    // Fase 4: catálogo de hasta 18 productos; nunca consulta otra empresa.
    Task<IReadOnlyList<AgenteCatalogoProducto>> ListarAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default);
}
