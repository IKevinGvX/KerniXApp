using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using KerniX.Services.Seguridad;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Seguridad;

/// <summary>
/// Recuperación compatible con seg.usuario + BCrypt.Net-Next +
/// seg.sp_usuario_restablecer_clave. El token en texto plano solo viaja por correo.
/// </summary>
public sealed class RecuperacionClaveService : IRecuperacionClaveService
{
    private readonly string _connectionString;
    private readonly IConfiguration _config;
    private readonly ILogger<RecuperacionClaveService> _logger;

    public RecuperacionClaveService(
        IConfiguration config,
        ILogger<RecuperacionClaveService> logger)
    {
        _config = config;
        _logger = logger;
        _connectionString = config.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
    }

    // No distingue públicamente entre cuentas existentes y desconocidas.
    public async Task SolicitarAsync(string correo, string ip, CancellationToken cancellationToken)
    {
        string email = (correo ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0 || email.Length > 150) return;
        string origen = string.IsNullOrWhiteSpace(ip) ? "sin-ip" : ip[..Math.Min(ip.Length, 60)];
        byte[] emailHash = SHA256.HashData(Encoding.UTF8.GetBytes("email:" + email));
        byte[] ipHash = SHA256.HashData(Encoding.UTF8.GetBytes("ip:" + origen));

        string? token = null;
        string? destinatario = null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var tx = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken))
        {
            // Limites conservadores, medidos en SQL Server y válidos para varias instancias.
            const string verificarLimites = @"
SELECT (SELECT COUNT_BIG(*) FROM seg.solicitud_recuperacion WITH(UPDLOCK,HOLDLOCK)
        WHERE correo_hash=@emailHash AND creado_utc>DATEADD(HOUR,-1,SYSUTCDATETIME())),
       (SELECT COUNT_BIG(*) FROM seg.solicitud_recuperacion WITH(UPDLOCK,HOLDLOCK)
        WHERE ip_hash=@ipHash AND creado_utc>DATEADD(HOUR,-1,SYSUTCDATETIME()));";
            long porCorreo, porIp;
            await using (var limit = new SqlCommand(verificarLimites, connection, tx))
            {
                limit.Parameters.Add("@emailHash", SqlDbType.Binary, 32).Value = emailHash;
                limit.Parameters.Add("@ipHash", SqlDbType.Binary, 32).Value = ipHash;
                await using var reader = await limit.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                porCorreo = reader.GetInt64(0);
                porIp = reader.GetInt64(1);
            }
            if (porCorreo >= 3 || porIp >= 10)
            {
                await tx.CommitAsync(cancellationToken);
                return;
            }

            const string insertarSolicitud = @"
INSERT INTO seg.solicitud_recuperacion(correo_hash,ip_hash)
VALUES(@emailHash,@ipHash);";
            await using (var audit = new SqlCommand(insertarSolicitud, connection, tx))
            {
                audit.Parameters.Add("@emailHash", SqlDbType.Binary, 32).Value = emailHash;
                audit.Parameters.Add("@ipHash", SqlDbType.Binary, 32).Value = ipHash;
                await audit.ExecuteNonQueryAsync(cancellationToken);
            }

            const string buscarUsuario = @"
SELECT TOP(1) id_usuario, correo
FROM seg.usuario WHERE estado=1 AND LOWER(LTRIM(RTRIM(correo)))=@correo;";
            int? idUsuario = null;
            await using (var buscar = new SqlCommand(buscarUsuario, connection, tx))
            {
                buscar.Parameters.Add("@correo", SqlDbType.NVarChar, 150).Value = email;
                await using var reader = await buscar.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    idUsuario = reader.GetInt32(0);
                    destinatario = reader.GetString(1);
                }
            }

            if (idUsuario.HasValue)
            {
                token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                byte[] hashToken = SHA256.HashData(Encoding.UTF8.GetBytes(token));
                const string guardar = @"
UPDATE seg.recuperacion_clave SET usado_utc=SYSUTCDATETIME()
 WHERE id_usuario=@usuario AND usado_utc IS NULL;
INSERT INTO seg.recuperacion_clave(id_usuario,token_hash,vence_utc)
 VALUES(@usuario,@hash,DATEADD(MINUTE,15,SYSUTCDATETIME()));";
                await using var cmd = new SqlCommand(guardar, connection, tx);
                cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario.Value;
                cmd.Parameters.Add("@hash", SqlDbType.Binary, 32).Value = hashToken;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
        }

        if (token is not null && destinatario is not null)
        {
            try
            {
                await EnviarCorreoAsync(destinatario, token, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Nunca registrar el enlace, token o credenciales SMTP.
                _logger.LogError(ex, "No se pudo enviar correo de recuperación de KerniX.");
                // El usuario ve un mensaje genérico, igual que para correos desconocidos.
            }
        }
    }

    public async Task<bool> TokenEsValidoAsync(string token, CancellationToken cancellationToken)
    {
        if (!TryHashToken(token, out byte[] hash)) return false;
        const string sql = @"
SELECT COUNT_BIG(1) FROM seg.recuperacion_clave r
JOIN seg.usuario u ON u.id_usuario=r.id_usuario
WHERE r.token_hash=@hash AND r.usado_utc IS NULL
  AND r.vence_utc>SYSUTCDATETIME() AND u.estado=1;";
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@hash", SqlDbType.Binary, 32).Value = hash;
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L) == 1;
    }

    public async Task<bool> RestablecerAsync(
        string token, string nuevaClave, CancellationToken cancellationToken)
    {
        if (!TryHashToken(token, out byte[] hash)) return false;
        if (string.IsNullOrWhiteSpace(nuevaClave) || nuevaClave.Length < 12 ||
            Encoding.UTF8.GetByteCount(nuevaClave) > 72) return false;

        // BCrypt es el mismo algoritmo que emplea UsuarioAdminDbService en KerniX.
        string nuevaClaveHash = BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor: 11);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        const string consumir = @"
UPDATE r SET usado_utc=SYSUTCDATETIME()
OUTPUT INSERTED.id_usuario
FROM seg.recuperacion_clave r
JOIN seg.usuario u ON u.id_usuario=r.id_usuario
WHERE r.token_hash=@hash AND r.usado_utc IS NULL
  AND r.vence_utc>SYSUTCDATETIME() AND u.estado=1;";
        int? idUsuario = null;
        await using (var cmd = new SqlCommand(consumir, connection, tx))
        {
            cmd.Parameters.Add("@hash", SqlDbType.Binary, 32).Value = hash;
            object? valor = await cmd.ExecuteScalarAsync(cancellationToken);
            if (valor is int id) idUsuario = id;
        }
        if (!idUsuario.HasValue)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        // Procedimiento real de KerniX, ya usado por UsuarioAdminDbService.
        await using (var cmd = new SqlCommand("seg.sp_usuario_restablecer_clave", connection, tx))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = idUsuario.Value;
            cmd.Parameters.Add("@clave_hash", SqlDbType.NVarChar, 500).Value = nuevaClaveHash;
            cmd.Parameters.Add("@requiere_cambio_clave", SqlDbType.Bit).Value = false;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        const string cerrarRestantes = @"
UPDATE seg.recuperacion_clave SET usado_utc=SYSUTCDATETIME()
WHERE id_usuario=@usuario AND usado_utc IS NULL;
IF OBJECT_ID(N'seg.sesion_web',N'U') IS NOT NULL
BEGIN
    UPDATE seg.sesion_web
       SET fecha_cierre_utc=SYSUTCDATETIME(), motivo_cierre=N'cambio_clave'
     WHERE id_usuario=@usuario AND fecha_cierre_utc IS NULL;
END;";
        await using (var cmd = new SqlCommand(cerrarRestantes, connection, tx))
        {
            cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario.Value;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        await tx.CommitAsync(cancellationToken);
        _logger.LogInformation("Contraseña restablecida para usuario ID={IdUsuario}.", idUsuario.Value);
        return true;
    }

    private static bool TryHashToken(string? token, out byte[] hash)
    {
        hash = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(token) || token.Length != 43) return false;
        try
        {
            byte[] raw = WebEncoders.Base64UrlDecode(token);
            if (raw.Length != 32) return false;
            hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private async Task EnviarCorreoAsync(
        string destinatario, string token, CancellationToken cancellationToken)
    {
        string host = _config["PasswordRecovery:Smtp:Host"] ?? string.Empty;
        string from = _config["PasswordRecovery:Smtp:From"] ?? string.Empty;
        string publicBaseUrl = (_config["PasswordRecovery:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
        string? user = _config["PasswordRecovery:Smtp:User"];
        string? pass = _config["PasswordRecovery:Smtp:Password"];
        int port = _config.GetValue<int>("PasswordRecovery:Smtp:Port", 587);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(publicBaseUrl) ||
            !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps &&
             !(baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback)))
        {
            throw new InvalidOperationException("Configuración de correo o URL pública incompleta/insegura.");
        }
        string enlace = $"{publicBaseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}";
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            throw new InvalidOperationException("Configura usuario y contraseña SMTP mediante configuración segura.");

        string html = CorreoRecuperacionTemplate.CrearHtml(enlace);
        string texto = CorreoRecuperacionTemplate.CrearTexto(enlace);

        using var email = new MailMessage
        {
            From = new MailAddress(from, "KerniX ERP", Encoding.UTF8),
            Subject = "KerniX ERP — Restablecer contraseña",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Body = texto,
            IsBodyHtml = false
        };
        email.To.Add(new MailAddress(destinatario));
        email.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(texto, Encoding.UTF8, MediaTypeNames.Text.Plain));
        email.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass)
        };
        await smtp.SendMailAsync(email, cancellationToken);
    }
}
