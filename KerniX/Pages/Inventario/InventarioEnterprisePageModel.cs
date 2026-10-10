using KerniX.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Inventario;

public abstract class InventarioEnterprisePageModel : PageModel
{
    // =========================================================
    // CONTEXTO DE SESIÓN COMPARTIDO
    // =========================================================

    public int IdUsuario { get; protected set; }

    public int IdEmpleado { get; protected set; }

    public int IdEmpresa { get; protected set; }

    public int IdSucursal { get; protected set; }

    public int IdAlmacen { get; protected set; }


    // =========================================================
    // CARGAR SESIÓN
    // =========================================================

    protected bool CargarSesion(
        bool requiereEmpleado = true)
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );

        IdEmpleado =
            ObtenerEnteroSesion(
                SessionKeys.IdEmpleado
            );

        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );

        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );

        IdAlmacen =
            ObtenerEnteroSesion(
                "IdAlmacen"
            );


        if (IdUsuario <= 0)
        {
            return false;
        }


        if (
            requiereEmpleado
            &&
            IdEmpleado <= 0
        )
        {
            return false;
        }


        // =====================================================
        // FALLBACK PARA TU ENTORNO LOCAL ACTUAL
        // =====================================================
        // En producción conviene quitar estos fallback y exigir
        // que Login siempre establezca empresa/sucursal.

        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }


        return true;
    }


    // =========================================================
    // REDIRECCIÓN LOGIN
    // =========================================================

    protected IActionResult IrLogin()
    {
        return RedirectToPage(
            "/Account/Login"
        );
    }


    // =========================================================
    // OBTENER ENTERO DE SESIÓN
    // =========================================================

    protected int ObtenerEnteroSesion(
        string clave)
    {
        int? valorInt =
            HttpContext
                .Session
                .GetInt32(
                    clave
                );


        if (valorInt.HasValue)
        {
            return valorInt.Value;
        }


        string? valorTexto =
            HttpContext
                .Session
                .GetString(
                    clave
                );


        return int.TryParse(
            valorTexto,
            out int numero
        )
            ? numero
            : 0;
    }
}
