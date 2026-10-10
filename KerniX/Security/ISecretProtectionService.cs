namespace KerniX.Services.Security;

public interface ISecretProtectionService
{
    string Proteger(string valor);

    string Desproteger(string valorProtegido);

    byte[] ProtegerBytes(byte[] datos);

    byte[] DesprotegerBytes(byte[] datosProtegidos);
}