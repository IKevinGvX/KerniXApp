namespace KerniX.Models.Sunat;

public sealed class SunatConfiguracion
{
    public int IdConfiguracionSunat { get; set; }

    public int IdEmpresa { get; set; }

    public string Ambiente { get; set; }
        = "BETA";

    public string Ruc { get; set; }
        = string.Empty;

    public string UsuarioSol { get; set; }
        = string.Empty;

    /*
        Estos campos contienen información
        CIFRADA cuando vienen desde SQL.
    */
    public string? ClaveSolCifrada { get; set; }

    public string? NombreCertificado { get; set; }

    public byte[]? CertificadoCifrado { get; set; }

    public string? PasswordCertificadoCifrado
    {
        get;
        set;
    }

    public bool EnviarAutomaticamente { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }
}