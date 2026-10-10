using Microsoft.AspNetCore.DataProtection;

namespace KerniX.Services.Security;

public sealed class SecretProtectionService
    : ISecretProtectionService
{
    private readonly IDataProtector
        _protectorTexto;

    private readonly IDataProtector
        _protectorArchivo;


    public SecretProtectionService(
        IDataProtectionProvider provider
    )
    {
        _protectorTexto =
            provider.CreateProtector(
                "KerniX.Sunat.Credenciales.v1"
            );

        _protectorArchivo =
            provider.CreateProtector(
                "KerniX.Sunat.Certificado.v1"
            );
    }


    public string Proteger(
        string valor
    )
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor no puede estar vacío.",
                nameof(valor)
            );
        }

        return _protectorTexto.Protect(
            valor
        );
    }


    public string Desproteger(
        string valorProtegido
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                valorProtegido
            )
        )
        {
            throw new ArgumentException(
                "El valor protegido no puede estar vacío.",
                nameof(valorProtegido)
            );
        }

        return _protectorTexto.Unprotect(
            valorProtegido
        );
    }


    public byte[] ProtegerBytes(
        byte[] datos
    )
    {
        if (
            datos is null ||
            datos.Length == 0
        )
        {
            throw new ArgumentException(
                "No existen datos para proteger.",
                nameof(datos)
            );
        }

        return _protectorArchivo.Protect(
            datos
        );
    }


    public byte[] DesprotegerBytes(
        byte[] datosProtegidos
    )
    {
        if (
            datosProtegidos is null ||
            datosProtegidos.Length == 0
        )
        {
            throw new ArgumentException(
                "No existen datos protegidos.",
                nameof(datosProtegidos)
            );
        }

        return _protectorArchivo.Unprotect(
            datosProtegidos
        );
    }
}