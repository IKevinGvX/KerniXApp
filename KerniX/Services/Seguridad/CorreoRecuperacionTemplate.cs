using System;
using System.Text.Encodings.Web;

namespace KerniX.Services.Seguridad;

/// <summary>
/// Mensaje HTML compatible con clientes de correo comunes: tablas + estilos inline.
/// No incluye imágenes remotas, CSS externo ni enlaces de seguimiento.
/// </summary>
public static class CorreoRecuperacionTemplate
{
    public static string CrearHtml(string enlace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enlace);
        string url = HtmlEncoder.Default.Encode(enlace);

        return $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>KerniX ERP - Recuperar contraseña</title>
        </head>
        <body style="margin:0;padding:0;background-color:#050b18;font-family:Arial,Helvetica,sans-serif;color:#f3f6ff;">
          <div style="display:none;font-size:1px;line-height:1px;color:#050b18;max-height:0;max-width:0;opacity:0;overflow:hidden;">
            Recupera tu acceso a KerniX ERP. Enlace seguro válido por 15 minutos.
          </div>
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color:#050b18;">
            <tr><td align="center" style="padding:36px 14px;">
              <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600" style="width:100%;max-width:600px;background-color:#0b1428;border:1px solid #786023;border-radius:16px;">
                <tr><td style="height:5px;background-color:#ffbc2d;font-size:0;line-height:0;">&nbsp;</td></tr>
                <tr><td align="center" style="padding:35px 38px 14px;">
                  <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
                    <td valign="middle" style="background-color:#141f3c;border:1px solid #d99d17;border-radius:12px;width:57px;height:57px;text-align:center;font-size:34px;font-weight:900;color:#ffd05f;">K</td>
                    <td valign="middle" style="padding-left:15px;text-align:left;">
                      <div style="font-size:26px;font-weight:800;color:#ffffff;letter-spacing:-1px;">KerniX <span style="color:#ffbf33;">ERP</span></div>
                      <div style="font-size:11px;line-height:1.5;color:#a5b4d1;">Sistema Inteligente de Gestión Empresarial</div>
                    </td>
                  </tr></table>
                </td></tr>
                <tr><td align="center" style="padding:20px 34px 12px;">
                  <div style="font-size:11px;font-weight:700;letter-spacing:2px;color:#ffca59;">CENTRO DE SEGURIDAD KERNIX</div>
                  <h1 style="font-size:30px;line-height:1.2;color:#ffffff;margin:14px 0 12px;">Recupera tu <span style="color:#ffca59;">contraseña</span></h1>
                  <p style="color:#c0cce1;font-size:15px;line-height:1.8;margin:0;">Recibimos una solicitud para restablecer la contraseña de tu cuenta KerniX ERP. Para continuar, selecciona el siguiente botón.</p>
                </td></tr>
                <tr><td align="center" style="padding:27px 34px 24px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td align="center" bgcolor="#f4ad18" style="border-radius:10px;background-color:#f4ad18;border:1px solid #ffe08a;">
                      <a href="{{url}}" style="display:inline-block;box-sizing:border-box;padding:17px 28px;font-size:15px;font-weight:800;line-height:1.3;color:#201400;text-decoration:none;">Restablecer mi contraseña &rarr;</a>
                    </td></tr></table>
                </td></tr>
                <tr><td align="center" style="padding:0 38px 24px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#101d34;border:1px solid #31476d;border-radius:10px;"><tr><td style="padding:16px;color:#c1cde4;font-size:13px;line-height:1.7;text-align:left;">
                    <strong style="color:#ffcc69;">Enlace de un solo uso</strong><br />Expira en <strong style="color:#ffffff;">15 minutos</strong> por tu seguridad.
                  </td></tr></table>
                </td></tr>
                <tr><td align="left" style="padding:0 38px 16px;">
                  <p style="font-size:12px;line-height:1.7;color:#aebbd2;">Si el botón no funciona, copia y pega esta dirección en tu navegador:</p>
                  <p style="font-size:12px;line-height:1.65;word-break:break-all;overflow-wrap:anywhere;margin:0;"><a href="{{url}}" style="color:#84c6ff;text-decoration:underline;">{{url}}</a></p>
                </td></tr>
                <tr><td align="left" style="padding:5px 38px 35px;">
                  <p style="font-size:12px;color:#aebbd2;line-height:1.7;">Si no solicitaste este cambio, ignora el mensaje. Nadie podrá cambiar tu contraseña sin acceso al enlace.</p>
                  <p style="font-size:12px;color:#ffcc69;line-height:1.6;margin:17px 0 0;">Equipo de Seguridad · KerniX ERP</p>
                </td></tr>
              </table>
              <p style="margin:23px 10px;color:#7185ab;font-size:11px;line-height:1.6;">Este es un mensaje automático de seguridad. No respondas a este correo.</p>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    public static string CrearTexto(string enlace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enlace);
        return "KerniX ERP — Recuperar contraseña\n\n" +
               "Recibimos una solicitud para restablecer la contraseña de tu cuenta.\n\n" +
               "Abre este enlace (válido 15 minutos, de un solo uso):\n" +
               enlace + "\n\n" +
               "Si no solicitaste el cambio, ignora este mensaje.\n\n" +
               "Equipo de Seguridad — KerniX ERP";
    }
}
