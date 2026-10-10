using KerniX.Models.Sunat;

namespace KerniX.Services.Sunat;

public interface ISunatConfiguracionDbService
{
    Task<SunatConfiguracion?> ObtenerAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default
    );

    Task GuardarAsync(
        SunatConfiguracion configuracion,
        CancellationToken cancellationToken = default
    );
}