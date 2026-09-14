"use strict";

document.addEventListener("DOMContentLoaded", () => {

    // =========================================================
    // ELEMENTOS
    // =========================================================

    const form =
        document.getElementById("loginForm");

    const usuario =
        document.getElementById("usuarioInput");

    const password =
        document.getElementById("kxPassword");

    const togglePassword =
        document.getElementById("kxTogglePassword");

    const loginButton =
        document.getElementById("loginButton");

    const loginButtonText =
        document.getElementById("loginButtonText");

    const loginSpinner =
        document.getElementById("loginSpinner");

    const loginArrow =
        document.getElementById("loginArrow");

    const usuarioError =
        document.getElementById("usuarioClientError");

    const passwordError =
        document.getElementById("passwordClientError");

    const serverError =
        document.getElementById("serverError");


    // =========================================================
    // ESTADO INICIAL
    // =========================================================

    restaurarBoton();


    // =========================================================
    // ERROR DEVUELTO POR ASP.NET
    // =========================================================

    if (serverError) {

        const mensaje =
            serverError.dataset.message?.trim();

        if (mensaje) {

            const esCredencialIncorrecta =
                esErrorDeCredenciales(mensaje);

            mostrarModal(
                esCredencialIncorrecta
                    ? "Credenciales incorrectas"
                    : "No se pudo iniciar sesión",

                mensaje,

                "danger",

                () => {

                    // Solo limpiamos cuando realmente
                    // son credenciales incorrectas.

                    if (esCredencialIncorrecta) {

                        usuario.value = "";
                        password.value = "";

                        usuario.focus();
                    }
                    else {

                        password.value = "";
                        password.focus();
                    }
                }
            );
        }
    }


    // =========================================================
    // LIMPIAR ERROR AL ESCRIBIR
    // =========================================================

    usuario?.addEventListener(
        "input",
        () => {

            quitarErrorUsuario();

        }
    );


    password?.addEventListener(
        "input",
        () => {

            quitarErrorPassword();

        }
    );


    // =========================================================
    // MOSTRAR / OCULTAR PASSWORD
    // =========================================================

    togglePassword?.addEventListener(
        "click",
        () => {

            if (!password) {
                return;
            }

            const oculto =
                password.type === "password";

            password.type =
                oculto
                    ? "text"
                    : "password";

            togglePassword.innerHTML =
                oculto
                    ? '<span uk-icon="icon: ban; ratio: .9"></span>'
                    : '<span uk-icon="icon: eye; ratio: .9"></span>';

            togglePassword.setAttribute(
                "aria-label",
                oculto
                    ? "Ocultar contraseña"
                    : "Mostrar contraseña"
            );

            if (window.UIkit) {
                UIkit.icon(togglePassword);
            }

            password.focus();
        }
    );


    // =========================================================
    // SUBMIT
    // =========================================================

    form?.addEventListener(
        "submit",
        event => {

            quitarTodosLosErrores();

            const valorUsuario =
                usuario?.value.trim() ?? "";

            const valorPassword =
                password?.value ?? "";


            // =================================================
            // LOS DOS VACÍOS
            // =================================================

            if (!valorUsuario && !valorPassword) {

                event.preventDefault();

                mostrarErrorUsuario(
                    "Ingresa tu usuario o correo electrónico."
                );

                mostrarErrorPassword(
                    "Ingresa tu contraseña."
                );

                mostrarModal(
                    "Datos incompletos",
                    "Ingresa tu usuario y contraseña.",
                    "warning"
                );

                usuario?.focus();

                return;
            }


            // =================================================
            // USUARIO VACÍO
            // =================================================

            if (!valorUsuario) {

                event.preventDefault();

                mostrarErrorUsuario(
                    "Ingresa tu usuario o correo electrónico."
                );

                mostrarModal(
                    "Usuario requerido",
                    "Ingresa tu usuario o correo electrónico.",
                    "warning"
                );

                usuario?.focus();

                return;
            }


            // =================================================
            // PASSWORD VACÍA
            // =================================================

            if (!valorPassword) {

                event.preventDefault();

                mostrarErrorPassword(
                    "Ingresa tu contraseña."
                );

                mostrarModal(
                    "Contraseña requerida",
                    "Ingresa tu contraseña.",
                    "warning"
                );

                password?.focus();

                return;
            }


            // =================================================
            // ENVÍO CORRECTO
            // =================================================

            mostrarCarga();

            /*
             * IMPORTANTE:
             *
             * NO llamamos:
             *
             * event.preventDefault()
             *
             * y tampoco hacemos:
             *
             * loginButton.disabled = true
             *
             * El navegador continúa con el POST normal
             * hacia LoginModel.OnPostAsync().
             */
        }
    );


    // =========================================================
    // BOTONES MICROSOFT / GOOGLE / SSO
    // =========================================================

    document
        .querySelectorAll(".kx-social-button")
        .forEach(button => {

            button.addEventListener(
                "click",
                () => {

                    const proveedor =
                        button.dataset.provider
                        ?? "Este método";

                    mostrarModal(
                        "Próximamente",
                        `${proveedor} todavía no está habilitado en KerniX ERP.`,
                        "info"
                    );

                }
            );

        });


    // =========================================================
    // ERROR USUARIO
    // =========================================================

    function mostrarErrorUsuario(
        mensaje) {

        if (!usuario || !usuarioError) {
            return;
        }

        usuario
            .closest(".kx-input-wrapper")
            ?.classList
            .add("kx-wrapper-error");

        usuarioError.hidden = false;

        usuarioError.innerHTML =
            `<span>⚠</span>
             <span>${escapeHtml(mensaje)}</span>`;
    }


    function quitarErrorUsuario() {

        usuario
            ?.closest(".kx-input-wrapper")
            ?.classList
            .remove("kx-wrapper-error");

        if (usuarioError) {

            usuarioError.hidden = true;
            usuarioError.innerHTML = "";
        }
    }


    // =========================================================
    // ERROR PASSWORD
    // =========================================================

    function mostrarErrorPassword(
        mensaje) {

        if (!password || !passwordError) {
            return;
        }

        password
            .closest(".kx-input-wrapper")
            ?.classList
            .add("kx-wrapper-error");

        passwordError.hidden = false;

        passwordError.innerHTML =
            `<span>⚠</span>
             <span>${escapeHtml(mensaje)}</span>`;
    }


    function quitarErrorPassword() {

        password
            ?.closest(".kx-input-wrapper")
            ?.classList
            .remove("kx-wrapper-error");

        if (passwordError) {

            passwordError.hidden = true;
            passwordError.innerHTML = "";
        }
    }


    function quitarTodosLosErrores() {

        quitarErrorUsuario();

        quitarErrorPassword();
    }


    // =========================================================
    // BOTÓN CARGANDO
    // =========================================================

    function mostrarCarga() {

        loginButton?.classList.add(
            "is-loading"
        );

        if (loginButtonText) {

            loginButtonText.textContent =
                "Verificando acceso...";
        }

        if (loginSpinner) {

            loginSpinner.hidden = false;
        }

        if (loginArrow) {

            loginArrow.hidden = true;
        }
    }


    function restaurarBoton() {

        if (loginButton) {

            loginButton.disabled = false;

            loginButton.classList.remove(
                "is-loading"
            );
        }

        if (loginButtonText) {

            loginButtonText.textContent =
                "Iniciar sesión";
        }

        if (loginSpinner) {

            loginSpinner.hidden = true;
        }

        if (loginArrow) {

            loginArrow.hidden = false;
        }
    }


    // =========================================================
    // IDENTIFICAR ERROR DE CREDENCIALES
    // =========================================================

    function esErrorDeCredenciales(
        mensaje) {

        const texto =
            mensaje.toLowerCase();

        return (
            texto.includes("usuario o contraseña") ||
            texto.includes("credenciales") ||
            texto.includes("incorrectos") ||
            texto.includes("incorrectas") ||
            texto.includes("login failed")
        );
    }


    // =========================================================
    // MODAL
    // =========================================================

    function mostrarModal(
        titulo,
        mensaje,
        tipo = "warning",
        onClose = null) {

        document
            .getElementById("kernixAlert")
            ?.remove();


        const overlay =
            document.createElement("div");


        overlay.id =
            "kernixAlert";


        overlay.className =
            `kx-alert-overlay kx-alert-${tipo}`;


        let icono = "⚠";

        if (tipo === "danger") {
            icono = "!";
        }

        if (tipo === "info") {
            icono = "i";
        }


        overlay.innerHTML = `

            <div class="kx-alert-modal"
                 role="dialog"
                 aria-modal="true">

                <div class="kx-alert-glow">
                </div>

                <button type="button"
                        class="kx-alert-close"
                        aria-label="Cerrar">

                    ×

                </button>

                <div class="kx-alert-icon">
                    ${icono}
                </div>

                <div class="kx-alert-title">
                    ${escapeHtml(titulo)}
                </div>

                <div class="kx-alert-message">
                    ${escapeHtml(mensaje)}
                </div>

                <button type="button"
                        class="kx-alert-button">

                    Entendido

                </button>

            </div>
        `;


        document.body.appendChild(
            overlay
        );


        requestAnimationFrame(() => {

            overlay.classList.add(
                "kx-alert-visible"
            );

        });


        let cerrado = false;


        const cerrar = () => {

            if (cerrado) {
                return;
            }

            cerrado = true;


            overlay.classList.remove(
                "kx-alert-visible"
            );


            window.setTimeout(
                () => {

                    overlay.remove();

                    if (
                        typeof onClose ===
                        "function"
                    ) {

                        onClose();
                    }

                },
                180
            );
        };


        overlay
            .querySelector(
                ".kx-alert-button"
            )
            ?.addEventListener(
                "click",
                cerrar
            );


        overlay
            .querySelector(
                ".kx-alert-close"
            )
            ?.addEventListener(
                "click",
                cerrar
            );


        overlay.addEventListener(
            "click",
            event => {

                if (event.target === overlay) {

                    cerrar();
                }
            }
        );


        document.addEventListener(
            "keydown",
            function escapeHandler(
                event) {

                if (event.key !== "Escape") {
                    return;
                }

                document.removeEventListener(
                    "keydown",
                    escapeHandler
                );

                cerrar();
            }
        );
    }


    // =========================================================
    // ESCAPE HTML
    // =========================================================

    function escapeHtml(value) {

        const element =
            document.createElement("div");

        element.textContent =
            value ?? "";

        return element.innerHTML;
    }

});