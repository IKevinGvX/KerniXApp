document.addEventListener(
    "DOMContentLoaded",
    function () {

        iniciarMenuVentas();

        iniciarMenuMovil();

        iniciarBuscadorGlobal();

        iniciarGraficoEstados();

    }
);
/* =========================================
   BOTÓN IMPRIMIR
   ========================================= */

.acciones - documento {
    display: flex;
    align - items: center;
    gap: 10px;
}

.btn - imprimir {
    padding: 11px 18px;
    border: 1px solid #f6b91f;
    border - radius: 8px;
    background: #f6b91f;
    color: #07111b;
    font - weight: 700;
    cursor: pointer;
}

.btn - imprimir:hover {
    background: #ffd45b;
}

/* =========================================
   IMPRESIÓN Y PDF
   ========================================= */

@media print {

    @page {
        size: A4;
        margin: 12mm;
    }

    body {
        background: white!important;
        color: black!important;
    }

    .page {
        width: 100 %;
        padding: 0;
        margin: 0;
    }

    .back,
    .acciones - documento {
        display: none!important;
    }

    .topbar {
        justify - content: flex - end;
    }

    .grid {
        display: block;
    }

    .panel {
        margin - bottom: 15px;
        border: 1px solid #cccccc;
        background: white!important;
        color: black!important;
        box - shadow: none!important;
        break-inside: avoid;
    }

    .field span,
    .field strong,
    .subtitle,
    .observation,
        th,
        td {
        color: black!important;
    }

    table {
        min - width: 0;
    }

    thead {
        display: table - header - group;
    }

    tr {
        break-inside: avoid;
    }

    .table - wrap {
        overflow: visible;
    }

    .status {
        border: 1px solid #888;
        background: white!important;
        color: black!important;
    }

    .total - final {
        color: black!important;
    }
}

/* ============================================================
   MENÚ VENTAS
   ============================================================ */

function iniciarMenuVentas() {

    const boton =
        document.getElementById(
            "ventasMenuButton"
        );

    if (!boton) {
        return;
    }


    boton.addEventListener(
        "click",
        function () {

            const seccion =
                boton.closest(
                    ".kx-menu-section"
                );

            if (!seccion) {
                return;
            }

            seccion.classList.toggle(
                "open"
            );

        }
    );

}


/* ============================================================
   MENÚ MÓVIL
   ============================================================ */

function iniciarMenuMovil() {

    const boton =
        document.getElementById(
            "mobileMenuButton"
        );

    const sidebar =
        document.getElementById(
            "sidebar"
        );

    const overlay =
        document.getElementById(
            "sidebarOverlay"
        );


    if (
        !boton ||
        !sidebar ||
        !overlay
    ) {
        return;
    }


    boton.addEventListener(
        "click",
        function () {

            sidebar.classList.add(
                "show"
            );

            overlay.classList.add(
                "show"
            );

        }
    );


    overlay.addEventListener(
        "click",
        cerrarSidebar
    );


    function cerrarSidebar() {

        sidebar.classList.remove(
            "show"
        );

        overlay.classList.remove(
            "show"
        );

    }

}


/* ============================================================
   BUSCADOR GLOBAL
   ============================================================ */

function iniciarBuscadorGlobal() {

    const input =
        document.getElementById(
            "globalSearch"
        );

    if (!input) {
        return;
    }


    document.addEventListener(
        "keydown",
        function (event) {

            if (
                event.ctrlKey &&
                event.key.toLowerCase() === "k"
            ) {

                event.preventDefault();

                input.focus();

                input.select();

            }

        }
    );


    input.addEventListener(
        "input",
        function () {

            const texto =
                normalizarTexto(
                    input.value
                );

            const filas =
                document.querySelectorAll(
                    ".kx-table tbody tr"
                );


            filas.forEach(
                function (fila) {

                    if (
                        fila.querySelector(
                            ".kx-empty"
                        )
                    ) {
                        return;
                    }


                    const contenido =
                        normalizarTexto(
                            fila.textContent
                        );


                    fila.style.display =
                        contenido.includes(texto)
                            ? ""
                            : "none";

                }
            );

        }
    );

}


function normalizarTexto(texto) {

    return (
        texto ||
        ""
    )
        .toLowerCase()
        .normalize("NFD")
        .replace(
            /[\u0300-\u036f]/g,
            ""
        );

}


/* ============================================================
   GRÁFICO ESTADOS
   ============================================================ */

function iniciarGraficoEstados() {

    const canvas =
        document.getElementById(
            "estadoChart"
        );

    if (
        !canvas ||
        typeof Chart === "undefined"
    ) {
        return;
    }


    const datos =
        window.kernixCotizacionChart;


    if (!datos) {
        return;
    }


    const labels =
        Array.isArray(datos.labels)
            ? datos.labels
            : [];


    const valores =
        Array.isArray(datos.valores)
            ? datos.valores
            : [];


    const colores =
        labels.map(
            function (estado) {

                const valor =
                    normalizarTexto(
                        estado
                    );


                if (
                    valor.includes(
                        "pendiente"
                    )
                ) {
                    return "#f6b91f";
                }


                if (
                    valor.includes(
                        "aprob"
                    )
                ) {
                    return "#35c94b";
                }


                if (
                    valor.includes(
                        "convert"
                    )
                ) {
                    return "#1687ff";
                }


                if (
                    valor.includes(
                        "rechaz"
                    )
                ) {
                    return "#f23b35";
                }


                return "#647c94";

            }
        );


    new Chart(
        canvas,
        {
            type: "doughnut",

            data: {

                labels: labels,

                datasets: [
                    {
                        data: valores,

                        backgroundColor:
                            colores,

                        borderColor:
                            colores,

                        borderWidth: 1,

                        hoverOffset: 5
                    }
                ]

            },

            options: {

                responsive: true,

                maintainAspectRatio: true,

                cutout: "69%",

                animation: {
                    duration: 700
                },

                plugins: {

                    legend: {
                        display: false
                    },

                    tooltip: {

                        backgroundColor:
                            "#071523",

                        borderColor:
                            "#274763",

                        borderWidth: 1,

                        titleColor:
                            "#ffffff",

                        bodyColor:
                            "#c7d5e3",

                        displayColors:
                            true

                    }

                }

            }

        }
    );

}