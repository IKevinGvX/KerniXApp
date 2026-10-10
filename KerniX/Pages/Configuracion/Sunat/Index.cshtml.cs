
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using KerniX.Models.Sunat;
using KerniX.Security;
using KerniX.Services.Security;
using KerniX.Services.Sunat;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Configuracion.Sunat;

public sealed class IndexModel : PageModel
{
    // =========================================================
    // CONFIGURACION
    // =========================================================

    private const long MaxCertificadoBytes =
        10 * 1024 * 1024;

    private const int IdEmpresaPredeterminada =
        1;

    private const int IdSucursalPredeterminada =
        1;


    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly ISunatConfiguracionDbService
        _db;

    private readonly ISecretProtectionService
        _proteccion;

    private readonly ILogger<IndexModel>
        _logger;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public IndexModel(
        ISunatConfiguracionDbService db,
        ISecretProtectionService proteccion,
        ILogger<IndexModel> logger
    )
    {
        _db =
            db;

        _proteccion =
            proteccion;

        _logger =
            logger;
    }


    // =========================================================
    // INPUT
    // =========================================================

    [BindProperty]
    public EntradaSunat Input
    {
        get;
        set;
    } = new();


    // =========================================================
    // ESTADO VISUAL
    // =========================================================

    public bool ClaveSolConfigurada
    {
        get;
        private set;
    }


    public bool CertificadoConfigurado
    {
        get;
        private set;
    }


    public bool PasswordCertificadoConfigurado
    {
        get;
        private set;
    }


    public string? NombreCertificadoActual
    {
        get;
        private set;
    }


    public string? CertificadoTitular
    {
        get;
        private set;
    }


    public DateTime? CertificadoVence
    {
        get;
        private set;
    }


    // =========================================================
    // CONTEXTO
    // =========================================================

    public int IdUsuarioActual
    {
        get;
        private set;
    }


    public int IdEmpresaActual
    {
        get;
        private set;
    }


    public int IdSucursalActual
    {
        get;
        private set;
    }


    // =========================================================
    // MENSAJES
    // =========================================================

    [TempData]
    public string? MensajeExito
    {
        get;
        set;
    }


    [TempData]
    public string? MensajeError
    {
        get;
        set;
    }

    [TempData]
    public string? ResultadoPrueba
    {
        get;
        set;
    }


    // =========================================================
    // MODELO DE ENTRADA
    // =========================================================

    public sealed class EntradaSunat
    {
        [Required(
            ErrorMessage =
                "El ambiente es obligatorio."
        )]
        public string Ambiente
        {
            get;
            set;
        } = "BETA";


        [Required(
            ErrorMessage =
                "El RUC es obligatorio."
        )]
        [RegularExpression(
            @"^\d{11}$",
            ErrorMessage =
                "El RUC debe contener 11 dígitos."
        )]
        public string Ruc
        {
            get;
            set;
        } = string.Empty;


        [Required(
            ErrorMessage =
                "El usuario SOL es obligatorio."
        )]
        [StringLength(
            100,
            ErrorMessage =
                "El usuario SOL no puede superar los 100 caracteres."
        )]
        public string UsuarioSol
        {
            get;
            set;
        } = string.Empty;


        [DataType(
            DataType.Password
        )]
        public string? ClaveSol
        {
            get;
            set;
        }


        [DataType(
            DataType.Password
        )]
        public string? PasswordCertificado
        {
            get;
            set;
        }


        public IFormFile? Certificado
        {
            get;
            set;
        }


        public bool EnviarAutomaticamente
        {
            get;
            set;
        }


        public bool Estado
        {
            get;
            set;
        } = true;
    }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult>
        OnGetAsync(
            CancellationToken ct
        )
    {
        int idUsuario =
            ObtenerIdUsuario();


        if (
            idUsuario <= 0
        )
        {
            _logger.LogWarning(
                "[SUNAT CONFIG] No existe IdUsuario valido en sesion."
            );


            return RedirectToPage(
                "/Account/Login"
            );
        }


        var contexto =
            ObtenerContextoEmpresaSucursal();


        IdUsuarioActual =
            idUsuario;

        IdEmpresaActual =
            contexto.IdEmpresa;

        IdSucursalActual =
            contexto.IdSucursal;


        _logger.LogInformation(
            "[SUNAT CONFIG] GET. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
            IdUsuarioActual,
            IdEmpresaActual,
            IdSucursalActual
        );


        try
        {
            await CargarAsync(
                IdEmpresaActual,
                ct
            );


            return Page();
        }
        catch (
            Exception ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT CONFIG] Error cargando. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
                IdUsuarioActual,
                IdEmpresaActual,
                IdSucursalActual
            );


            MensajeError =
                "No se pudo cargar la configuración SUNAT.";


            return Page();
        }
    }


    // =========================================================
    // POST - GUARDAR
    // =========================================================

    public async Task<IActionResult>
        OnPostGuardarAsync(
            CancellationToken ct
        )
    {
        int idUsuario =
            ObtenerIdUsuario();


        if (
            idUsuario <= 0
        )
        {
            _logger.LogWarning(
                "[SUNAT CONFIG] POST sin usuario valido."
            );


            return RedirectToPage(
                "/Account/Login"
            );
        }


        var contexto =
            ObtenerContextoEmpresaSucursal();


        int idEmpresa =
            contexto.IdEmpresa;

        int idSucursal =
            contexto.IdSucursal;


        IdUsuarioActual =
            idUsuario;

        IdEmpresaActual =
            idEmpresa;

        IdSucursalActual =
            idSucursal;


        _logger.LogInformation(
            "[SUNAT CONFIG] POST GUARDAR. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
            idUsuario,
            idEmpresa,
            idSucursal
        );


        // =====================================================
        // NORMALIZAR
        // =====================================================

        Input.Ambiente =
            (
                Input.Ambiente
                ??
                string.Empty
            )
            .Trim()
            .ToUpperInvariant();


        Input.Ruc =
            (
                Input.Ruc
                ??
                string.Empty
            )
            .Trim();


        Input.UsuarioSol =
            (
                Input.UsuarioSol
                ??
                string.Empty
            )
            .Trim();


        // =====================================================
        // VALIDAR AMBIENTE
        // =====================================================

        if (
            Input.Ambiente
            is not
            (
                "BETA"
                or
                "PRODUCCION"
            )
        )
        {
            ModelState.AddModelError(
                nameof(
                    Input.Ambiente
                ),
                "Ambiente SUNAT no válido."
            );
        }


        // =====================================================
        // OBTENER CONFIGURACION EXISTENTE
        // =====================================================

        SunatConfiguracion? existente =
            await _db
                .ObtenerAsync(
                    idEmpresa,
                    ct
                );


        bool tieneClaveSol =
            !string.IsNullOrWhiteSpace(
                existente?
                    .ClaveSolCifrada
            );


        bool tieneCertificado =
            existente?
                .CertificadoCifrado
            is
            {
                Length: > 0
            };


        bool tienePasswordCertificado =
            !string.IsNullOrWhiteSpace(
                existente?
                    .PasswordCertificadoCifrado
            );


        // =====================================================
        // VALIDAR CLAVE SOL
        // =====================================================

        if (
            !tieneClaveSol
            &&
            string.IsNullOrWhiteSpace(
                Input.ClaveSol
            )
        )
        {
            ModelState.AddModelError(
                nameof(
                    Input.ClaveSol
                ),
                "Registre la Clave SOL."
            );
        }


        // =====================================================
        // VALIDAR CERTIFICADO
        // =====================================================

        if (
            !tieneCertificado
            &&
            Input.Certificado
            is null
        )
        {
            ModelState.AddModelError(
                nameof(
                    Input.Certificado
                ),
                "Debe cargar el certificado digital .pfx o .p12."
            );
        }


        // =====================================================
        // VALIDAR PASSWORD CERTIFICADO
        // =====================================================

        if (
            !tienePasswordCertificado
            &&
            string.IsNullOrWhiteSpace(
                Input.PasswordCertificado
            )
        )
        {
            ModelState.AddModelError(
                nameof(
                    Input.PasswordCertificado
                ),
                "Registre la contraseña del certificado."
            );
        }


        // =====================================================
        // VALIDAR ARCHIVO CERTIFICADO
        // =====================================================

        if (
            Input.Certificado
            is not null
        )
        {
            string extension =
                Path
                    .GetExtension(
                        Input
                            .Certificado
                            .FileName
                    )
                    .ToLowerInvariant();


            if (
                extension
                is not
                (
                    ".pfx"
                    or
                    ".p12"
                )
            )
            {
                ModelState.AddModelError(
                    nameof(
                        Input.Certificado
                    ),
                    "Solo se permiten certificados .pfx o .p12."
                );
            }


            if (
                Input.Certificado.Length <= 0
                ||
                Input.Certificado.Length >
                MaxCertificadoBytes
            )
            {
                ModelState.AddModelError(
                    nameof(
                        Input.Certificado
                    ),
                    "El certificado debe pesar entre 1 byte y 10 MB."
                );
            }
        }


        // =====================================================
        // MODELSTATE
        // =====================================================

        if (
            !ModelState.IsValid
        )
        {
            foreach (
                var item
                in
                ModelState
            )
            {
                foreach (
                    var error
                    in
                    item.Value.Errors
                )
                {
                    _logger.LogWarning(
                        "[SUNAT CONFIG] MODELSTATE | Campo={Campo} Error={Error}",
                        item.Key,
                        error.ErrorMessage
                    );
                }
            }


            await AplicarEstadoVisualAsync(
                idEmpresa,
                ct
            );


            return Page();
        }


        // =====================================================
        // CREAR CONFIGURACION
        // =====================================================

        var configuracion =
            new SunatConfiguracion
            {
                IdEmpresa =
                    idEmpresa,

                Ambiente =
                    Input.Ambiente,

                Ruc =
                    Input.Ruc,

                UsuarioSol =
                    Input.UsuarioSol,

                EnviarAutomaticamente =
                    Input
                        .EnviarAutomaticamente,

                Estado =
                    Input.Estado
            };


        // =====================================================
        // CIFRAR CLAVE SOL
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                Input.ClaveSol
            )
        )
        {
            configuracion
                .ClaveSolCifrada =
                    _proteccion
                        .Proteger(
                            Input.ClaveSol
                        );
        }


        // =====================================================
        // CIFRAR PASSWORD PFX
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                Input.PasswordCertificado
            )
        )
        {
            configuracion
                .PasswordCertificadoCifrado =
                    _proteccion
                        .Proteger(
                            Input
                                .PasswordCertificado
                        );
        }


        // =====================================================
        // PROCESAR CERTIFICADO
        // =====================================================

        if (
            Input.Certificado
            is not null
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    Input.PasswordCertificado
                )
            )
            {
                ModelState.AddModelError(
                    nameof(
                        Input.PasswordCertificado
                    ),
                    "Para reemplazar el certificado debe ingresar también su contraseña."
                );


                await AplicarEstadoVisualAsync(
                    idEmpresa,
                    ct
                );


                return Page();
            }


            await using var memory =
                new MemoryStream();


            await Input
                .Certificado
                .CopyToAsync(
                    memory,
                    ct
                );


            byte[] certificadoOriginal =
                memory.ToArray();


            // =================================================
            // VALIDAR CERTIFICADO
            // =================================================

            try
            {
                using var certificado =
                    new X509Certificate2(
                        certificadoOriginal,
                        Input
                            .PasswordCertificado,
                        X509KeyStorageFlags
                            .EphemeralKeySet
                        |
                        X509KeyStorageFlags
                            .Exportable
                    );


                if (
                    !certificado
                        .HasPrivateKey
                )
                {
                    ModelState.AddModelError(
                        nameof(
                            Input.Certificado
                        ),
                        "El certificado no contiene una clave privada."
                    );


                    await AplicarEstadoVisualAsync(
                        idEmpresa,
                        ct
                    );


                    return Page();
                }


                if (
                    certificado
                        .NotAfter
                        .ToUniversalTime()
                    <=
                    DateTime.UtcNow
                )
                {
                    ModelState.AddModelError(
                        nameof(
                            Input.Certificado
                        ),
                        $"El certificado venció el {certificado.NotAfter:dd/MM/yyyy}."
                    );


                    await AplicarEstadoVisualAsync(
                        idEmpresa,
                        ct
                    );


                    return Page();
                }
            }
            catch (
                CryptographicException ex
            )
            {
                _logger.LogWarning(
                    ex,
                    "[SUNAT CONFIG] PFX invalido. Empresa={Empresa}",
                    idEmpresa
                );


                ModelState.AddModelError(
                    nameof(
                        Input.Certificado
                    ),
                    "No se pudo abrir el certificado. Verifique el archivo y su contraseña."
                );


                await AplicarEstadoVisualAsync(
                    idEmpresa,
                    ct
                );


                return Page();
            }


            // =================================================
            // NOMBRE CERTIFICADO
            // =================================================

            configuracion
                .NombreCertificado =
                    Path
                        .GetFileName(
                            Input
                                .Certificado
                                .FileName
                        );


            // =================================================
            // CIFRAR CERTIFICADO
            // =================================================

            configuracion
                .CertificadoCifrado =
                    _proteccion
                        .ProtegerBytes(
                            certificadoOriginal
                        );
        }


        // =====================================================
        // GUARDAR EN SQL
        // =====================================================

        try
        {
            await _db
                .GuardarAsync(
                    configuracion,
                    ct
                );


            MensajeExito =
                "Configuración SUNAT guardada correctamente.";


            _logger.LogInformation(
                "[SUNAT CONFIG] Guardado correcto. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal} Ambiente={Ambiente}",
                idUsuario,
                idEmpresa,
                idSucursal,
                Input.Ambiente
            );


            return RedirectToPage();
        }
        catch (
            Exception ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT CONFIG] Error guardando. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
                idUsuario,
                idEmpresa,
                idSucursal
            );


            ModelState.AddModelError(
                string.Empty,
                "No se pudo guardar la configuración SUNAT."
            );


            await AplicarEstadoVisualAsync(
                idEmpresa,
                ct
            );


            return Page();
        }
    }


    // =========================================================
    // POST - PROBAR CERTIFICADO
    // =========================================================

    public async Task<IActionResult>
        OnPostProbarCertificadoAsync(
            CancellationToken ct
        )
    {
        int idUsuario =
            ObtenerIdUsuario();


        if (
            idUsuario <= 0
        )
        {
            _logger.LogWarning(
                "[SUNAT CONFIG] Probar certificado sin usuario valido."
            );


            return RedirectToPage(
                "/Account/Login"
            );
        }


        var contexto =
            ObtenerContextoEmpresaSucursal();


        int idEmpresa =
            contexto.IdEmpresa;

        int idSucursal =
            contexto.IdSucursal;


        IdUsuarioActual =
            idUsuario;

        IdEmpresaActual =
            idEmpresa;

        IdSucursalActual =
            idSucursal;


        _logger.LogInformation(
            "[SUNAT CONFIG] PROBAR CERTIFICADO. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
            idUsuario,
            idEmpresa,
            idSucursal
        );


        SunatConfiguracion? configuracion =
            await _db
                .ObtenerAsync(
                    idEmpresa,
                    ct
                );


        if (
            configuracion?
                .CertificadoCifrado
            is not
            {
                Length: > 0
            }
            ||
            string.IsNullOrWhiteSpace(
                configuracion
                    .PasswordCertificadoCifrado
            )
        )
        {
            MensajeError =
                "No hay certificado y contraseña configurados.";


            return RedirectToPage();
        }


        try
        {
            // =================================================
            // DESCIFRAR CERTIFICADO
            // =================================================

            byte[] certificadoBytes =
                _proteccion
                    .DesprotegerBytes(
                        configuracion
                            .CertificadoCifrado
                    );


            // =================================================
            // DESCIFRAR PASSWORD
            // =================================================

            string password =
                _proteccion
                    .Desproteger(
                        configuracion
                            .PasswordCertificadoCifrado!
                    );


            // =================================================
            // ABRIR CERTIFICADO
            // =================================================

            using var certificado =
                new X509Certificate2(
                    certificadoBytes,
                    password,
                    X509KeyStorageFlags
                        .EphemeralKeySet
                );


            // =================================================
            // VALIDAR CLAVE PRIVADA
            // =================================================

            if (
                !certificado
                    .HasPrivateKey
            )
            {
                MensajeError =
                    "El certificado no contiene una clave privada.";


                return RedirectToPage();
            }


            // =================================================
            // VALIDAR VENCIMIENTO
            // =================================================

            if (
                certificado
                    .NotAfter
                    .ToUniversalTime()
                <=
                DateTime.UtcNow
            )
            {
                MensajeError =
                    $"El certificado venció el {certificado.NotAfter:dd/MM/yyyy}.";


                return RedirectToPage();
            }


            // =================================================
            // TITULAR
            // =================================================

            string titular =
                certificado
                    .GetNameInfo(
                        X509NameType
                            .SimpleName,
                        false
                    );


            MensajeExito =
                $"Certificado válido. " +
                $"Titular: {titular}. " +
                $"Vence: {certificado.NotAfter:dd/MM/yyyy}.";


            _logger.LogInformation(
                "[SUNAT CONFIG] Certificado valido. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal} Titular={Titular}",
                idUsuario,
                idEmpresa,
                idSucursal,
                titular
            );
        }
        catch (
            CryptographicException ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT CONFIG] Error criptografico leyendo certificado. Empresa={Empresa}",
                idEmpresa
            );


            MensajeError =
                "No fue posible descifrar o abrir el certificado guardado.";
        }
        catch (
            Exception ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT CONFIG] Error comprobando certificado. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
                idUsuario,
                idEmpresa,
                idSucursal
            );


            MensajeError =
                "No fue posible abrir el certificado guardado.";
        }


        return RedirectToPage();
    }



    // =========================================================
    // POST - PROBAR CONFIGURACION COMPLETA
    // =========================================================

    public async Task<IActionResult>
        OnPostProbarConfiguracionAsync(
            CancellationToken ct
        )
    {
        int idUsuario =
            ObtenerIdUsuario();

        if (idUsuario <= 0)
        {
            _logger.LogWarning(
                "[SUNAT TEST] Prueba completa sin usuario valido."
            );

            return RedirectToPage(
                "/Account/Login"
            );
        }

        var contexto =
            ObtenerContextoEmpresaSucursal();

        int idEmpresa =
            contexto.IdEmpresa;

        int idSucursal =
            contexto.IdSucursal;

        IdUsuarioActual =
            idUsuario;

        IdEmpresaActual =
            idEmpresa;

        IdSucursalActual =
            idSucursal;

        _logger.LogInformation(
            "[SUNAT TEST] Inicio prueba completa. Usuario={Usuario} Empresa={Empresa} Sucursal={Sucursal}",
            idUsuario,
            idEmpresa,
            idSucursal
        );

        try
        {
            SunatConfiguracion? config =
                await _db.ObtenerAsync(
                    idEmpresa,
                    ct
                );

            if (config is null)
            {
                MensajeError =
                    "La empresa todavía no tiene configuración SUNAT.";

                return RedirectToPage();
            }

            if (!config.Estado)
            {
                MensajeError =
                    "La configuración SUNAT está desactivada.";

                return RedirectToPage();
            }

            string ambiente =
                (
                    config.Ambiente
                    ??
                    string.Empty
                )
                .Trim()
                .ToUpperInvariant();

            if (
                ambiente
                is not
                (
                    "BETA"
                    or
                    "PRODUCCION"
                )
            )
            {
                MensajeError =
                    "El ambiente SUNAT configurado no es válido.";

                return RedirectToPage();
            }

            string ruc =
                (
                    config.Ruc
                    ??
                    string.Empty
                )
                .Trim();

            if (
                ruc.Length != 11
                ||
                !ruc.All(char.IsDigit)
            )
            {
                MensajeError =
                    "El RUC configurado no es válido.";

                return RedirectToPage();
            }

            string usuarioSol =
                (
                    config.UsuarioSol
                    ??
                    string.Empty
                )
                .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    usuarioSol
                )
            )
            {
                MensajeError =
                    "No existe Usuario SOL configurado.";

                return RedirectToPage();
            }

            if (
                string.IsNullOrWhiteSpace(
                    config.ClaveSolCifrada
                )
            )
            {
                MensajeError =
                    "No existe Clave SOL configurada.";

                return RedirectToPage();
            }

            string claveSol =
                _proteccion.Desproteger(
                    config.ClaveSolCifrada
                );

            if (
                string.IsNullOrWhiteSpace(
                    claveSol
                )
            )
            {
                MensajeError =
                    "La Clave SOL no pudo ser recuperada.";

                return RedirectToPage();
            }

            if (
                config.CertificadoCifrado
                is not
                {
                    Length: > 0
                }
            )
            {
                MensajeError =
                    "No existe certificado digital configurado.";

                return RedirectToPage();
            }

            if (
                string.IsNullOrWhiteSpace(
                    config.PasswordCertificadoCifrado
                )
            )
            {
                MensajeError =
                    "No existe contraseña del certificado.";

                return RedirectToPage();
            }

            byte[] certificadoBytes =
                _proteccion.DesprotegerBytes(
                    config.CertificadoCifrado
                );

            string passwordCertificado =
                _proteccion.Desproteger(
                    config.PasswordCertificadoCifrado
                );

            if (
                string.IsNullOrWhiteSpace(
                    passwordCertificado
                )
            )
            {
                MensajeError =
                    "La contraseña del certificado no pudo ser recuperada.";

                return RedirectToPage();
            }

            using var certificado =
                new X509Certificate2(
                    certificadoBytes,
                    passwordCertificado,
                    X509KeyStorageFlags
                        .EphemeralKeySet
                );

            if (
                !certificado.HasPrivateKey
            )
            {
                MensajeError =
                    "El certificado existe pero no contiene clave privada.";

                return RedirectToPage();
            }

            DateTime ahora =
                DateTime.Now;

            if (
                ahora <
                certificado.NotBefore
            )
            {
                MensajeError =
                    $"El certificado todavía no es válido. Inicio: {certificado.NotBefore:dd/MM/yyyy}.";

                return RedirectToPage();
            }

            if (
                ahora >
                certificado.NotAfter
            )
            {
                MensajeError =
                    $"El certificado está vencido desde {certificado.NotAfter:dd/MM/yyyy}.";

                return RedirectToPage();
            }

            string titular =
                certificado.GetNameInfo(
                    X509NameType.SimpleName,
                    false
                );

            string thumbprint =
                certificado.Thumbprint
                ??
                string.Empty;

            ResultadoPrueba =
                $"Empresa={idEmpresa} | " +
                $"Sucursal={idSucursal} | " +
                $"Ambiente={ambiente} | " +
                $"RUC={ruc} | " +
                $"Usuario SOL configurado | " +
                $"Clave SOL cifrada y recuperada correctamente | " +
                $"Certificado={config.NombreCertificado ?? "Sin nombre"} | " +
                $"Titular={titular} | " +
                $"Vence={certificado.NotAfter:dd/MM/yyyy}";

            MensajeExito =
                "Prueba local SUNAT completada correctamente.";

            _logger.LogInformation(
                "[SUNAT TEST] OK. Empresa={Empresa} Sucursal={Sucursal} RUC={Ruc} Ambiente={Ambiente} Certificado={Certificado} Titular={Titular} Vence={Vence} Thumbprint={Thumbprint}",
                idEmpresa,
                idSucursal,
                ruc,
                ambiente,
                config.NombreCertificado,
                titular,
                certificado.NotAfter,
                thumbprint
            );

            return RedirectToPage();
        }
        catch (
            CryptographicException ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT TEST] Error criptográfico. Empresa={Empresa} Sucursal={Sucursal}",
                idEmpresa,
                idSucursal
            );

            MensajeError =
                "Falló el descifrado o la apertura del certificado.";

            return RedirectToPage();
        }
        catch (
            Exception ex
        )
        {
            _logger.LogError(
                ex,
                "[SUNAT TEST] Error general. Empresa={Empresa} Sucursal={Sucursal}",
                idEmpresa,
                idSucursal
            );

            MensajeError =
                $"La prueba SUNAT falló: {ex.Message}";

            return RedirectToPage();
        }
    }


    // =========================================================
    // CARGAR CONFIGURACION
    // =========================================================

    private async Task
        CargarAsync(
            int idEmpresa,
            CancellationToken ct
        )
    {
        SunatConfiguracion? configuracion =
            await _db
                .ObtenerAsync(
                    idEmpresa,
                    ct
                );


        // =====================================================
        // SIN CONFIGURACION
        // =====================================================

        if (
            configuracion
            is null
        )
        {
            Input =
                new EntradaSunat
                {
                    Ambiente =
                        "BETA",

                    EnviarAutomaticamente =
                        false,

                    Estado =
                        true
                };


            ClaveSolConfigurada =
                false;

            CertificadoConfigurado =
                false;

            PasswordCertificadoConfigurado =
                false;

            NombreCertificadoActual =
                null;

            CertificadoTitular =
                null;

            CertificadoVence =
                null;


            return;
        }


        // =====================================================
        // DATOS NO SENSIBLES
        // =====================================================

        Input =
            new EntradaSunat
            {
                Ambiente =
                    configuracion
                        .Ambiente,

                Ruc =
                    configuracion
                        .Ruc,

                UsuarioSol =
                    configuracion
                        .UsuarioSol,

                EnviarAutomaticamente =
                    configuracion
                        .EnviarAutomaticamente,

                Estado =
                    configuracion
                        .Estado
            };


        /*
            NO SE CARGAN:

            Input.ClaveSol
            Input.PasswordCertificado

            Nunca devolvemos secretos al navegador.
        */


        await AplicarEstadoVisualAsync(
            idEmpresa,
            ct
        );
    }


    // =========================================================
    // ESTADO VISUAL
    // =========================================================

    private async Task
        AplicarEstadoVisualAsync(
            int idEmpresa,
            CancellationToken ct
        )
    {
        SunatConfiguracion? configuracion =
            await _db
                .ObtenerAsync(
                    idEmpresa,
                    ct
                );


        if (
            configuracion
            is null
        )
        {
            ClaveSolConfigurada =
                false;

            CertificadoConfigurado =
                false;

            PasswordCertificadoConfigurado =
                false;

            NombreCertificadoActual =
                null;

            CertificadoTitular =
                null;

            CertificadoVence =
                null;


            return;
        }


        // =====================================================
        // ESTADOS
        // =====================================================

        ClaveSolConfigurada =
            !string.IsNullOrWhiteSpace(
                configuracion
                    .ClaveSolCifrada
            );


        CertificadoConfigurado =
            configuracion
                .CertificadoCifrado
            is
            {
                Length: > 0
            };


        PasswordCertificadoConfigurado =
            !string.IsNullOrWhiteSpace(
                configuracion
                    .PasswordCertificadoCifrado
            );


        NombreCertificadoActual =
            configuracion
                .NombreCertificado;


        // =====================================================
        // LEER METADATA DEL CERTIFICADO
        // =====================================================

        if (
            CertificadoConfigurado
            &&
            PasswordCertificadoConfigurado
        )
        {
            try
            {
                byte[] certificadoBytes =
                    _proteccion
                        .DesprotegerBytes(
                            configuracion
                                .CertificadoCifrado!
                        );


                string password =
                    _proteccion
                        .Desproteger(
                            configuracion
                                .PasswordCertificadoCifrado!
                        );


                using var certificado =
                    new X509Certificate2(
                        certificadoBytes,
                        password,
                        X509KeyStorageFlags
                            .EphemeralKeySet
                    );


                CertificadoTitular =
                    certificado
                        .GetNameInfo(
                            X509NameType
                                .SimpleName,
                            false
                        );


                CertificadoVence =
                    certificado
                        .NotAfter;
            }
            catch (
                Exception ex
            )
            {
                _logger.LogWarning(
                    ex,
                    "[SUNAT CONFIG] No se pudo leer metadata del certificado. Empresa={Empresa}",
                    idEmpresa
                );


                CertificadoTitular =
                    "No disponible";

                CertificadoVence =
                    null;
            }
        }
    }


    // =========================================================
    // USUARIO AUTENTICADO
    // =========================================================

    private int
        ObtenerIdUsuario()
    {
        /*
            IMPORTANTE:

            Usamos SessionKeys.IdUsuario,
            igual que el resto de KerniX.
        */

        int idUsuario =
            HttpContext
                .Session
                .GetInt32(
                    SessionKeys.IdUsuario
                )
            ??
            0;


        _logger.LogInformation(
            "[SUNAT CONFIG] SessionKeys.IdUsuario={IdUsuario}",
            idUsuario
        );


        return idUsuario;
    }


    // =========================================================
    // EMPRESA / SUCURSAL
    // =========================================================

    private (
        int IdEmpresa,
        int IdSucursal
    )
        ObtenerContextoEmpresaSucursal()
    {
        // =====================================================
        // EMPRESA
        // =====================================================

        int idEmpresa =
            HttpContext
                .Session
                .GetInt32(
                    "IdEmpresa"
                )
            ??
            0;


        if (
            idEmpresa <= 0
        )
        {
            idEmpresa =
                IdEmpresaPredeterminada;


            HttpContext
                .Session
                .SetInt32(
                    "IdEmpresa",
                    idEmpresa
                );


            _logger.LogWarning(
                "[SUNAT CONFIG] IdEmpresa no existia. Se usa temporalmente Empresa={Empresa}.",
                idEmpresa
            );
        }


        // =====================================================
        // SUCURSAL
        // =====================================================

        int idSucursal =
            HttpContext
                .Session
                .GetInt32(
                    "IdSucursal"
                )
            ??
            0;


        if (
            idSucursal <= 0
        )
        {
            idSucursal =
                IdSucursalPredeterminada;


            HttpContext
                .Session
                .SetInt32(
                    "IdSucursal",
                    idSucursal
                );


            _logger.LogWarning(
                "[SUNAT CONFIG] IdSucursal no existia. Se usa temporalmente Sucursal={Sucursal}.",
                idSucursal
            );
        }


        return (
            idEmpresa,
            idSucursal
        );
    }
}