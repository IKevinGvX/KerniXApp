// ============================================================
// KERNIX - DASHBOARD VENTAS
// GRÁFICOS 3D + CONFIGURACIÓN
// ============================================================

let graficoVentas = null;
let graficoCategorias = null;
let graficoPagos = null;


// ============================================================
// CONFIGURACIÓN PREDETERMINADA
// ============================================================

const KERNIX_CONFIG_DEFAULT = {

    ventas: {
        color: "#f8c95c",
        areaColor: "#32a7ff",
        min: 0,
        max: null
    },

    categorias: {
        grosor: 62,
        profundidad: 13
    },

    pagos: {
        yape: "#8e35ff",
        plin: "#00d9a5",
        efectivo: "#f8c23d",
        tarjeta: "#1398ff",
        transferencia: "#15d8ef",
        otros: "#a66cff",
        min: 0,
        max: null,
        profundidad: 11
    }

};


let configuracionKernix =
    cargarConfiguracionKernix();


// ============================================================
// INICIO
// ============================================================

document.addEventListener(
    "DOMContentLoaded",
    function () {

        if (typeof Chart === "undefined") {

            console.error(
                "Chart.js no se encuentra cargado."
            );

            return;
        }


        configurarChart();

        cargarInputsConfiguracion();

        configurarBotonesPaneles();

        configurarCambiosTiempoReal();

        configurarBotonesRestablecer();

        configurarBotonGuardarPagos();

        crearTodosLosGraficos();

        actualizarValoresRangos();

    }
);


// ============================================================
// CONFIGURACIÓN CHART
// ============================================================

function configurarChart() {

    Chart.defaults.color =
        "#8da0b5";

    Chart.defaults.font.family =
        "Inter, Segoe UI, Arial, sans-serif";

    Chart.defaults.font.size =
        11;

    Chart.defaults.animation.duration =
        650;

}


// ============================================================
// CREAR TODOS
// ============================================================

function crearTodosLosGraficos() {

    const datos =
        window.kernixVentasData || {};


    crearGraficoVentas3D(
        datos.ventasPeriodo || []
    );


    crearGraficoCategorias3D(
        datos.categorias || []
    );


    crearGraficoPagos3D(
        datos.metodosPago || []
    );


    renderizarLeyendaPagos(
        datos.metodosPago || []
    );

}


// ============================================================
// PLUGIN SOMBRA
// ============================================================

const pluginSombra3D = {

    id:
        "kxSombra3D",

    beforeDatasetDraw(chart) {

        const ctx =
            chart.ctx;

        ctx.save();

        ctx.shadowColor =
            "rgba(0,0,0,.48)";

        ctx.shadowBlur =
            18;

        ctx.shadowOffsetX =
            6;

        ctx.shadowOffsetY =
            10;
    },

    afterDatasetDraw(chart) {

        chart.ctx.restore();

    }

};


// ============================================================
// PLUGIN BARRA 3D
// ============================================================

const pluginBarras3D = {

    id:
        "kxBarras3D",

    afterDatasetsDraw(chart) {

        if (
            chart.config.type !==
            "bar"
        ) {
            return;
        }


        const ctx =
            chart.ctx;

        const meta =
            chart.getDatasetMeta(0);


        const profundidad =
            Number(
                configuracionKernix
                    .pagos
                    .profundidad || 11
            );


        meta.data.forEach(
            function (
                barra,
                index
            ) {

                const props =
                    barra.getProps(
                        [
                            "x",
                            "y",
                            "base",
                            "width"
                        ],
                        true
                    );


                const x =
                    props.x;

                const y =
                    props.y;

                const base =
                    props.base;

                const width =
                    props.width;


                const izquierda =
                    x - width / 2;

                const derecha =
                    x + width / 2;


                const color =
                    chart
                        .data
                        .datasets[0]
                        .backgroundColor[index];


                // ============================================
                // CARA LATERAL
                // ============================================

                ctx.save();

                ctx.beginPath();

                ctx.moveTo(
                    derecha,
                    y
                );

                ctx.lineTo(
                    derecha + profundidad,
                    y - profundidad
                );

                ctx.lineTo(
                    derecha + profundidad,
                    base - profundidad
                );

                ctx.lineTo(
                    derecha,
                    base
                );

                ctx.closePath();

                ctx.fillStyle =
                    oscurecerColor(
                        color,
                        0.42
                    );

                ctx.fill();

                ctx.restore();


                // ============================================
                // CARA SUPERIOR
                // ============================================

                ctx.save();

                ctx.beginPath();

                ctx.moveTo(
                    izquierda,
                    y
                );

                ctx.lineTo(
                    izquierda + profundidad,
                    y - profundidad
                );

                ctx.lineTo(
                    derecha + profundidad,
                    y - profundidad
                );

                ctx.lineTo(
                    derecha,
                    y
                );

                ctx.closePath();


                const brillo =
                    ctx.createLinearGradient(
                        izquierda,
                        y,
                        derecha,
                        y - profundidad
                    );


                brillo.addColorStop(
                    0,
                    aclararColor(
                        color,
                        0.40
                    )
                );

                brillo.addColorStop(
                    1,
                    "#ffffff"
                );


                ctx.globalAlpha =
                    0.82;

                ctx.fillStyle =
                    brillo;

                ctx.fill();

                ctx.restore();


                // ============================================
                // REFLEJO FRONTAL
                // ============================================

                ctx.save();


                const reflejo =
                    ctx.createLinearGradient(
                        izquierda,
                        y,
                        derecha,
                        base
                    );


                reflejo.addColorStop(
                    0,
                    "rgba(255,255,255,.45)"
                );

                reflejo.addColorStop(
                    0.25,
                    "rgba(255,255,255,.10)"
                );

                reflejo.addColorStop(
                    1,
                    "rgba(255,255,255,0)"
                );


                ctx.fillStyle =
                    reflejo;


                ctx.fillRect(
                    izquierda + 4,
                    y + 5,
                    Math.max(
                        3,
                        width * 0.18
                    ),
                    Math.max(
                        0,
                        base - y - 10
                    )
                );

                ctx.restore();

            }
        );

    }

};


// ============================================================
// GRÁFICO VENTAS 3D
// ============================================================

function crearGraficoVentas3D(
    datos
) {

    const canvas =
        document.getElementById(
            "ventasChart"
        );


    if (!canvas) {
        return;
    }


    const ctx =
        canvas.getContext(
            "2d"
        );


    const colorLinea =
        configuracionKernix
            .ventas
            .color;


    const colorArea =
        configuracionKernix
            .ventas
            .areaColor;


    const fondo =
        ctx.createLinearGradient(
            0,
            0,
            0,
            330
        );


    fondo.addColorStop(
        0,
        hexARgba(
            colorArea,
            0.62
        )
    );


    fondo.addColorStop(
        0.4,
        hexARgba(
            colorArea,
            0.26
        )
    );


    fondo.addColorStop(
        0.8,
        hexARgba(
            colorArea,
            0.07
        )
    );


    fondo.addColorStop(
        1,
        hexARgba(
            colorArea,
            0
        )
    );


    graficoVentas =
        new Chart(
            canvas,
            {

                type:
                    "line",

                data: {

                    labels:
                        datos.map(
                            x =>
                                x.etiqueta
                        ),

                    datasets: [
                        {

                            label:
                                "Ventas",

                            data:
                                datos.map(
                                    x =>
                                        Number(
                                            x.total || 0
                                        )
                                ),

                            borderColor:
                                colorLinea,

                            backgroundColor:
                                fondo,

                            borderWidth:
                                4,

                            pointRadius:
                                5,

                            pointHoverRadius:
                                8,

                            pointBackgroundColor:
                                colorLinea,

                            pointBorderColor:
                                "#ffffff",

                            pointBorderWidth:
                                2,

                            tension:
                                0.42,

                            fill:
                                true

                        }
                    ]

                },


                plugins: [
                    pluginSombra3D
                ],


                options: {

                    responsive:
                        true,

                    maintainAspectRatio:
                        false,


                    interaction: {

                        mode:
                            "index",

                        intersect:
                            false

                    },


                    plugins: {

                        legend: {

                            display:
                                false

                        },


                        tooltip: {

                            backgroundColor:
                                "rgba(4,13,26,.96)",

                            titleColor:
                                "#ffffff",

                            bodyColor:
                                "#dbe7f5",

                            borderColor:
                                hexARgba(
                                    colorLinea,
                                    .55
                                ),

                            borderWidth:
                                1,

                            padding:
                                13,

                            displayColors:
                                false,


                            callbacks: {

                                label(context) {

                                    return (
                                        "Ventas: " +
                                        formatearMoneda(
                                            Number(
                                                context.raw || 0
                                            )
                                        )
                                    );

                                }

                            }

                        }

                    },


                    scales: {

                        x: {

                            offset:
                                true,

                            grid: {

                                color:
                                    "rgba(70,120,180,.07)"

                            },

                            border: {

                                color:
                                    "rgba(120,170,220,.18)"

                            },

                            ticks: {

                                color:
                                    "#8aa0b8",

                                maxRotation:
                                    0,

                                autoSkip:
                                    true,

                                maxTicksLimit:
                                    10

                            }

                        },


                        y: {

                            beginAtZero:
                                true,

                            min:
                                configuracionKernix
                                    .ventas
                                    .min,

                            max:
                                configuracionKernix
                                    .ventas
                                    .max ??
                                undefined,

                            grid: {

                                color:
                                    "rgba(70,120,180,.10)"

                            },

                            border: {

                                color:
                                    "rgba(120,170,220,.18)"

                            },

                            ticks: {

                                color:
                                    "#8aa0b8",

                                callback(value) {

                                    return resumirMoneda(
                                        Number(value)
                                    );

                                }

                            }

                        }

                    }

                }

            }
        );

}


// ============================================================
// GRÁFICO CATEGORÍAS 3D
// ============================================================

function crearGraficoCategorias3D(
    datos
) {

    const canvas =
        document.getElementById(
            "categoriasChart"
        );


    if (!canvas) {
        return;
    }


    const ctx =
        canvas.getContext(
            "2d"
        );


    const colores = [

        "#f8c95c",
        "#32a7ff",
        "#a66cff",
        "#3af2a5",
        "#24e6ff",
        "#ff7188",
        "#6688ff",
        "#df9d46"

    ];


    const pluginDona3D = {

        id:
            "kxDona3D",


        beforeDatasetsDraw(chart) {

            const meta =
                chart.getDatasetMeta(0);


            const profundidad =
                Number(
                    configuracionKernix
                        .categorias
                        .profundidad || 13
                );


            for (
                let capa = profundidad;
                capa > 0;
                capa--
            ) {

                meta.data.forEach(
                    function (
                        arco,
                        index
                    ) {

                        const props =
                            arco.getProps(
                                [
                                    "x",
                                    "y",
                                    "startAngle",
                                    "endAngle",
                                    "innerRadius",
                                    "outerRadius"
                                ],
                                true
                            );


                        ctx.save();


                        ctx.beginPath();


                        ctx.arc(
                            props.x,
                            props.y + capa,
                            props.outerRadius,
                            props.startAngle,
                            props.endAngle
                        );


                        ctx.arc(
                            props.x,
                            props.y + capa,
                            props.innerRadius,
                            props.endAngle,
                            props.startAngle,
                            true
                        );


                        ctx.closePath();


                        ctx.fillStyle =
                            oscurecerColor(
                                colores[
                                index %
                                colores.length
                                ],
                                0.48
                            );


                        ctx.fill();


                        ctx.restore();

                    }
                );

            }

        }

    };


    graficoCategorias =
        new Chart(
            canvas,
            {

                type:
                    "doughnut",

                data: {

                    labels:
                        datos.map(
                            x =>
                                x.categoria
                        ),

                    datasets: [
                        {

                            data:
                                datos.map(
                                    x =>
                                        Number(
                                            x.total || 0
                                        )
                                ),

                            backgroundColor:
                                colores,

                            borderColor:
                                "#07101d",

                            borderWidth:
                                3,

                            hoverOffset:
                                12

                        }
                    ]

                },


                plugins: [

                    pluginDona3D,
                    pluginSombra3D

                ],


                options: {

                    responsive:
                        true,

                    maintainAspectRatio:
                        false,


                    cutout:
                        configuracionKernix
                            .categorias
                            .grosor +
                        "%",


                    rotation:
                        -85,


                    plugins: {

                        legend: {

                            position:
                                "bottom",

                            labels: {

                                color:
                                    "#9aadc0",

                                usePointStyle:
                                    true,

                                pointStyle:
                                    "circle",

                                boxWidth:
                                    8,

                                boxHeight:
                                    8,

                                padding:
                                    16

                            }

                        },


                        tooltip: {

                            backgroundColor:
                                "rgba(4,13,26,.96)",

                            borderColor:
                                "rgba(248,201,92,.55)",

                            borderWidth:
                                1,

                            padding:
                                12,


                            callbacks: {

                                label(context) {

                                    return (
                                        context.label +
                                        ": " +
                                        formatearMoneda(
                                            Number(
                                                context.raw || 0
                                            )
                                        )
                                    );

                                }

                            }

                        }

                    }

                }

            }
        );

}


// ============================================================
// GRÁFICO PAGOS 3D
// ============================================================

function crearGraficoPagos3D(
    datos
) {

    const canvas =
        document.getElementById(
            "pagosChart"
        );


    if (!canvas) {
        return;
    }


    const colores =
        datos.map(
            x =>
                obtenerColorMetodoPago(
                    x.metodo
                )
        );


    graficoPagos =
        new Chart(
            canvas,
            {

                type:
                    "bar",

                data: {

                    labels:
                        datos.map(
                            x =>
                                x.metodo
                        ),

                    datasets: [
                        {

                            label:
                                "Monto",

                            data:
                                datos.map(
                                    x =>
                                        Number(
                                            x.total || 0
                                        )
                                ),

                            backgroundColor:
                                colores,

                            borderColor:
                                colores,

                            borderWidth:
                                1,

                            borderRadius:
                                4,

                            borderSkipped:
                                false,

                            maxBarThickness:
                                52,

                            barPercentage:
                                .66,

                            categoryPercentage:
                                .72

                        }
                    ]

                },


                plugins: [

                    pluginBarras3D,
                    pluginSombra3D

                ],


                options: {

                    responsive:
                        true,

                    maintainAspectRatio:
                        false,


                    layout: {

                        padding: {

                            top:
                                18,

                            right:
                                22,

                            bottom:
                                4,

                            left:
                                4

                        }

                    },


                    plugins: {

                        legend: {

                            display:
                                false

                        },


                        tooltip: {

                            backgroundColor:
                                "rgba(4,13,26,.96)",

                            titleColor:
                                "#ffffff",

                            bodyColor:
                                "#dbe7f5",

                            borderColor:
                                "rgba(255,255,255,.15)",

                            borderWidth:
                                1,

                            padding:
                                12,

                            displayColors:
                                true,


                            callbacks: {

                                label(context) {

                                    return (
                                        "Total: " +
                                        formatearMoneda(
                                            Number(
                                                context.raw || 0
                                            )
                                        )
                                    );

                                }

                            }

                        }

                    },


                    scales: {

                        x: {

                            grid: {

                                display:
                                    false

                            },

                            border: {

                                color:
                                    "rgba(140,180,220,.16)"

                            },

                            ticks: {

                                color:
                                    "#a6b5c5",

                                font: {

                                    size:
                                        11,

                                    weight:
                                        "600"

                                }

                            }

                        },


                        y: {

                            beginAtZero:
                                true,

                            min:
                                configuracionKernix
                                    .pagos
                                    .min,

                            max:
                                configuracionKernix
                                    .pagos
                                    .max ??
                                undefined,

                            grid: {

                                color:
                                    "rgba(100,140,185,.08)"

                            },

                            border: {

                                color:
                                    "rgba(140,180,220,.16)"

                            },

                            ticks: {

                                color:
                                    "#71859b",

                                callback(value) {

                                    return resumirMoneda(
                                        Number(value)
                                    );

                                }

                            }

                        }

                    }

                }

            }
        );

}


// ============================================================
// LEYENDA PAGOS
// ============================================================

function renderizarLeyendaPagos(
    datos
) {

    const contenedor =
        document.getElementById(
            "pagosLegend"
        );


    if (!contenedor) {
        return;
    }


    contenedor.innerHTML =
        "";


    datos.forEach(
        function (item) {

            const tipo =
                normalizarMetodoPago(
                    item.metodo
                );


            const color =
                obtenerColorMetodoPago(
                    item.metodo
                );


            const elemento =
                document.createElement(
                    "div"
                );


            elemento.className =
                "kxpagobadge";


            elemento.style.setProperty(
                "--pago-color",
                color
            );


            const logo =
                obtenerLogoMetodo(
                    tipo
                );


            elemento.innerHTML = `

                <span class="kxpagologo ${tipo}">
                    ${logo}
                </span>

                <span class="kxpagoinfo">

                    <strong>
                        ${escaparHtml(item.metodo)}
                    </strong>

                    <small>
                        ${formatearMoneda(item.total)}
                    </small>

                </span>

            `;


            contenedor.appendChild(
                elemento
            );

        }
    );

}


// ============================================================
// NORMALIZAR MÉTODO
// ============================================================

function normalizarMetodoPago(
    nombre
) {

    const metodo =
        String(
            nombre || ""
        )
            .trim()
            .toLowerCase();


    if (
        metodo.includes("yape")
    ) {
        return "yape";
    }


    if (
        metodo.includes("plin")
    ) {
        return "plin";
    }


    if (
        metodo.includes("efectivo")
    ) {
        return "efectivo";
    }


    if (
        metodo.includes("tarjeta") ||
        metodo.includes("visa") ||
        metodo.includes("master")
    ) {
        return "tarjeta";
    }


    if (
        metodo.includes("transfer")
    ) {
        return "transferencia";
    }


    return "otros";

}


// ============================================================
// LOGO VISUAL
// ============================================================

function obtenerLogoMetodo(
    tipo
) {

    switch (tipo) {

        case "yape":
            return "Y";

        case "plin":
            return "P";

        case "efectivo":
            return "S/";

        case "tarjeta":
            return "▣";

        case "transferencia":
            return "⇄";

        default:
            return "•";

    }

}


// ============================================================
// COLOR MÉTODO
// ============================================================

function obtenerColorMetodoPago(
    nombre
) {

    const tipo =
        normalizarMetodoPago(
            nombre
        );


    switch (tipo) {

        case "yape":

            return configuracionKernix
                .pagos
                .yape;


        case "plin":

            return configuracionKernix
                .pagos
                .plin;


        case "efectivo":

            return configuracionKernix
                .pagos
                .efectivo;


        case "tarjeta":

            return configuracionKernix
                .pagos
                .tarjeta;


        case "transferencia":

            return configuracionKernix
                .pagos
                .transferencia;


        default:

            return configuracionKernix
                .pagos
                .otros;

    }

}


// ============================================================
// CONFIG PANELES
// ============================================================

function configurarBotonesPaneles() {

    document
        .querySelectorAll(
            ".kxconfiggrafico"
        )
        .forEach(
            function (boton) {

                boton.addEventListener(
                    "click",
                    function () {

                        abrirPanelConfiguracion(
                            boton.dataset.config
                        );

                    }
                );

            }
        );

}


// ============================================================
// ABRIR PANEL
// ============================================================

function abrirPanelConfiguracion(
    tipo
) {

    const mapa = {

        ventas:
            "configVentas",

        categorias:
            "configCategorias",

        pagos:
            "configPagos"

    };


    const id =
        mapa[tipo];


    const panel =
        document.getElementById(
            id
        );


    if (!panel) {
        return;
    }


    const abrir =
        panel.hidden;


    document
        .querySelectorAll(
            ".kxconfigpanel"
        )
        .forEach(
            x =>
                x.hidden = true
        );


    panel.hidden =
        !abrir;

}


// ============================================================
// INPUTS
// ============================================================

function cargarInputsConfiguracion() {

    asignarValor(
        "ventasColor",
        configuracionKernix.ventas.color
    );

    asignarValor(
        "ventasAreaColor",
        configuracionKernix.ventas.areaColor
    );

    asignarValor(
        "ventasMin",
        configuracionKernix.ventas.min
    );

    asignarValor(
        "ventasMax",
        configuracionKernix.ventas.max ?? ""
    );


    asignarValor(
        "categoriasGrosor",
        configuracionKernix.categorias.grosor
    );

    asignarValor(
        "categoriasProfundidad",
        configuracionKernix.categorias.profundidad
    );


    asignarValor(
        "colorYape",
        configuracionKernix.pagos.yape
    );

    asignarValor(
        "colorPlin",
        configuracionKernix.pagos.plin
    );

    asignarValor(
        "colorEfectivo",
        configuracionKernix.pagos.efectivo
    );

    asignarValor(
        "colorTarjeta",
        configuracionKernix.pagos.tarjeta
    );

    asignarValor(
        "colorTransferencia",
        configuracionKernix.pagos.transferencia
    );

    asignarValor(
        "pagosMin",
        configuracionKernix.pagos.min
    );

    asignarValor(
        "pagosMax",
        configuracionKernix.pagos.max ?? ""
    );

    asignarValor(
        "pagosProfundidad",
        configuracionKernix.pagos.profundidad
    );

}


// ============================================================
// EVENTOS INPUT
// ============================================================

function configurarCambiosTiempoReal() {

    [
        "ventasColor",
        "ventasAreaColor",
        "ventasMin",
        "ventasMax"
    ]
        .forEach(
            id =>
                escucharCambio(
                    id,
                    actualizarConfiguracionVentas
                )
        );


    [
        "categoriasGrosor",
        "categoriasProfundidad"
    ]
        .forEach(
            id =>
                escucharCambio(
                    id,
                    actualizarConfiguracionCategorias
                )
        );


    [
        "colorYape",
        "colorPlin",
        "colorEfectivo",
        "colorTarjeta",
        "colorTransferencia",
        "pagosMin",
        "pagosMax",
        "pagosProfundidad"
    ]
        .forEach(
            id =>
                escucharCambio(
                    id,
                    actualizarConfiguracionPagos
                )
        );

}


// ============================================================
// ESCUCHAR CAMBIO
// ============================================================

function escucharCambio(
    id,
    callback
) {

    const elemento =
        document.getElementById(
            id
        );


    if (!elemento) {
        return;
    }


    elemento.addEventListener(
        "input",
        callback
    );

}


// ============================================================
// ACTUALIZAR VENTAS
// ============================================================

function actualizarConfiguracionVentas() {

    configuracionKernix
        .ventas
        .color =
        obtenerValor(
            "ventasColor",
            "#f8c95c"
        );


    configuracionKernix
        .ventas
        .areaColor =
        obtenerValor(
            "ventasAreaColor",
            "#32a7ff"
        );


    configuracionKernix
        .ventas
        .min =
        obtenerNumero(
            "ventasMin",
            0
        );


    configuracionKernix
        .ventas
        .max =
        obtenerNumeroNullable(
            "ventasMax"
        );


    guardarConfiguracionKernix();


    reconstruirGraficoVentas();

}


// ============================================================
// ACTUALIZAR CATEGORÍAS
// ============================================================

function actualizarConfiguracionCategorias() {

    configuracionKernix
        .categorias
        .grosor =
        obtenerNumero(
            "categoriasGrosor",
            62
        );


    configuracionKernix
        .categorias
        .profundidad =
        obtenerNumero(
            "categoriasProfundidad",
            13
        );


    guardarConfiguracionKernix();


    actualizarValoresRangos();


    reconstruirGraficoCategorias();

}


// ============================================================
// ACTUALIZAR PAGOS
// ============================================================

function actualizarConfiguracionPagos() {

    configuracionKernix.pagos.yape =
        obtenerValor(
            "colorYape",
            "#8e35ff"
        );


    configuracionKernix.pagos.plin =
        obtenerValor(
            "colorPlin",
            "#00d9a5"
        );


    configuracionKernix.pagos.efectivo =
        obtenerValor(
            "colorEfectivo",
            "#f8c23d"
        );


    configuracionKernix.pagos.tarjeta =
        obtenerValor(
            "colorTarjeta",
            "#1398ff"
        );


    configuracionKernix.pagos.transferencia =
        obtenerValor(
            "colorTransferencia",
            "#15d8ef"
        );


    configuracionKernix.pagos.min =
        obtenerNumero(
            "pagosMin",
            0
        );


    configuracionKernix.pagos.max =
        obtenerNumeroNullable(
            "pagosMax"
        );


    configuracionKernix.pagos.profundidad =
        obtenerNumero(
            "pagosProfundidad",
            11
        );


    guardarConfiguracionKernix();


    actualizarValoresRangos();


    reconstruirGraficoPagos();


    renderizarLeyendaPagos(
        window.kernixVentasData
            ?.metodosPago || []
    );

}


// ============================================================
// RECONSTRUIR
// ============================================================

function reconstruirGraficoVentas() {

    if (graficoVentas) {

        graficoVentas.destroy();

        graficoVentas =
            null;

    }


    crearGraficoVentas3D(
        window.kernixVentasData
            ?.ventasPeriodo || []
    );

}


function reconstruirGraficoCategorias() {

    if (graficoCategorias) {

        graficoCategorias.destroy();

        graficoCategorias =
            null;

    }


    crearGraficoCategorias3D(
        window.kernixVentasData
            ?.categorias || []
    );

}


function reconstruirGraficoPagos() {

    if (graficoPagos) {

        graficoPagos.destroy();

        graficoPagos =
            null;

    }


    crearGraficoPagos3D(
        window.kernixVentasData
            ?.metodosPago || []
    );

}


// ============================================================
// RESTABLECER
// ============================================================

function configurarBotonesRestablecer() {

    const ventas =
        document.getElementById(
            "restablecerConfigVentas"
        );


    if (ventas) {

        ventas.addEventListener(
            "click",
            function () {

                configuracionKernix.ventas =
                    copiarObjeto(
                        KERNIX_CONFIG_DEFAULT
                            .ventas
                    );


                guardarConfiguracionKernix();

                cargarInputsConfiguracion();

                reconstruirGraficoVentas();

            }
        );

    }


    const categorias =
        document.getElementById(
            "restablecerConfigCategorias"
        );


    if (categorias) {

        categorias.addEventListener(
            "click",
            function () {

                configuracionKernix.categorias =
                    copiarObjeto(
                        KERNIX_CONFIG_DEFAULT
                            .categorias
                    );


                guardarConfiguracionKernix();

                cargarInputsConfiguracion();

                actualizarValoresRangos();

                reconstruirGraficoCategorias();

            }
        );

    }


    const pagos =
        document.getElementById(
            "restablecerConfigPagos"
        );


    if (pagos) {

        pagos.addEventListener(
            "click",
            function () {

                configuracionKernix.pagos =
                    copiarObjeto(
                        KERNIX_CONFIG_DEFAULT
                            .pagos
                    );


                guardarConfiguracionKernix();

                cargarInputsConfiguracion();

                actualizarValoresRangos();

                reconstruirGraficoPagos();


                renderizarLeyendaPagos(
                    window.kernixVentasData
                        ?.metodosPago || []
                );

            }
        );

    }

}


// ============================================================
// BOTÓN GUARDAR
// ============================================================

function configurarBotonGuardarPagos() {

    const boton =
        document.getElementById(
            "guardarConfigPagos"
        );


    if (!boton) {
        return;
    }


    boton.addEventListener(
        "click",
        function () {

            actualizarConfiguracionPagos();


            const panel =
                document.getElementById(
                    "configPagos"
                );


            if (panel) {

                panel.hidden =
                    true;

            }


            if (
                typeof UIkit !==
                "undefined"
            ) {

                UIkit.notification({

                    message:
                        "Configuración guardada",

                    status:
                        "success",

                    pos:
                        "top-right",

                    timeout:
                        1800

                });

            }

        }
    );

}


// ============================================================
// RANGOS VISUALES
// ============================================================

function actualizarValoresRangos() {

    const grosor =
        document.getElementById(
            "categoriasGrosorValor"
        );


    if (grosor) {

        grosor.textContent =
            configuracionKernix
                .categorias
                .grosor +
            "%";

    }


    const profundidadCategoria =
        document.getElementById(
            "categoriasProfundidadValor"
        );


    if (profundidadCategoria) {

        profundidadCategoria.textContent =
            configuracionKernix
                .categorias
                .profundidad;

    }


    const profundidadPago =
        document.getElementById(
            "pagosProfundidadValor"
        );


    if (profundidadPago) {

        profundidadPago.textContent =
            configuracionKernix
                .pagos
                .profundidad;

    }

}


// ============================================================
// LOCAL STORAGE
// ============================================================

function cargarConfiguracionKernix() {

    try {

        const guardado =
            localStorage.getItem(
                "kernix_dashboard_config"
            );


        if (!guardado) {

            return copiarObjeto(
                KERNIX_CONFIG_DEFAULT
            );

        }


        const config =
            JSON.parse(
                guardado
            );


        return {

            ventas: {

                ...KERNIX_CONFIG_DEFAULT
                    .ventas,

                ...(config.ventas || {})

            },


            categorias: {

                ...KERNIX_CONFIG_DEFAULT
                    .categorias,

                ...(config.categorias || {})

            },


            pagos: {

                ...KERNIX_CONFIG_DEFAULT
                    .pagos,

                ...(config.pagos || {})

            }

        };

    }
    catch {

        return copiarObjeto(
            KERNIX_CONFIG_DEFAULT
        );

    }

}


// ============================================================
// GUARDAR LOCAL STORAGE
// ============================================================

function guardarConfiguracionKernix() {

    localStorage.setItem(
        "kernix_dashboard_config",
        JSON.stringify(
            configuracionKernix
        )
    );

}


// ============================================================
// VALORES
// ============================================================

function asignarValor(
    id,
    valor
) {

    const elemento =
        document.getElementById(
            id
        );


    if (elemento) {

        elemento.value =
            valor;

    }

}


function obtenerValor(
    id,
    predeterminado
) {

    const elemento =
        document.getElementById(
            id
        );


    if (!elemento) {

        return predeterminado;

    }


    return (
        elemento.value ||
        predeterminado
    );

}


function obtenerNumero(
    id,
    predeterminado
) {

    const elemento =
        document.getElementById(
            id
        );


    if (!elemento) {

        return predeterminado;

    }


    const numero =
        Number(
            elemento.value
        );


    return Number.isFinite(
        numero
    )
        ? numero
        : predeterminado;

}


function obtenerNumeroNullable(
    id
) {

    const elemento =
        document.getElementById(
            id
        );


    if (
        !elemento ||
        elemento.value === ""
    ) {

        return null;

    }


    const numero =
        Number(
            elemento.value
        );


    return Number.isFinite(
        numero
    )
        ? numero
        : null;

}


// ============================================================
// MONEDA
// ============================================================

function formatearMoneda(
    valor
) {

    return Number(
        valor || 0
    )
        .toLocaleString(
            "es-PE",
            {

                style:
                    "currency",

                currency:
                    "PEN",

                minimumFractionDigits:
                    2,

                maximumFractionDigits:
                    2

            }
        );

}


// ============================================================
// MONEDA REDUCIDA
// ============================================================

function resumirMoneda(
    valor
) {

    const numero =
        Number(
            valor || 0
        );


    if (
        numero >=
        1000000
    ) {

        return (
            "S/ " +
            (
                numero /
                1000000
            ).toFixed(1) +
            "M"
        );

    }


    if (
        numero >=
        1000
    ) {

        return (
            "S/ " +
            (
                numero /
                1000
            ).toFixed(1) +
            "K"
        );

    }


    return (
        "S/ " +
        numero.toFixed(0)
    );

}


// ============================================================
// HEX -> RGBA
// ============================================================

function hexARgba(
    hex,
    alpha
) {

    let valor =
        String(
            hex ||
            "#000000"
        )
            .replace(
                "#",
                ""
            );


    if (
        valor.length ===
        3
    ) {

        valor =
            valor
                .split("")
                .map(
                    x =>
                        x + x
                )
                .join("");

    }


    const numero =
        parseInt(
            valor,
            16
        );


    const r =
        (numero >> 16) &
        255;


    const g =
        (numero >> 8) &
        255;


    const b =
        numero &
        255;


    return (
        `rgba(${r},${g},${b},${alpha})`
    );

}


// ============================================================
// OSCURECER
// ============================================================

function oscurecerColor(
    hex,
    cantidad
) {

    if (
        !hex ||
        !hex.startsWith("#")
    ) {

        return "#111827";

    }


    const color =
        hex.replace(
            "#",
            ""
        );


    const numero =
        parseInt(
            color,
            16
        );


    let r =
        (numero >> 16) &
        255;


    let g =
        (numero >> 8) &
        255;


    let b =
        numero &
        255;


    r =
        Math.max(
            0,
            Math.floor(
                r *
                (1 - cantidad)
            )
        );


    g =
        Math.max(
            0,
            Math.floor(
                g *
                (1 - cantidad)
            )
        );


    b =
        Math.max(
            0,
            Math.floor(
                b *
                (1 - cantidad)
            )
        );


    return (
        "#" +
        [r, g, b]
            .map(
                x =>
                    x
                        .toString(16)
                        .padStart(
                            2,
                            "0"
                        )
            )
            .join("")
    );

}


// ============================================================
// ACLARAR
// ============================================================

function aclararColor(
    hex,
    cantidad
) {

    const color =
        hex.replace(
            "#",
            ""
        );


    const numero =
        parseInt(
            color,
            16
        );


    let r =
        (numero >> 16) &
        255;


    let g =
        (numero >> 8) &
        255;


    let b =
        numero &
        255;


    r =
        Math.min(
            255,
            Math.floor(
                r +
                (255 - r) *
                cantidad
            )
        );


    g =
        Math.min(
            255,
            Math.floor(
                g +
                (255 - g) *
                cantidad
            )
        );


    b =
        Math.min(
            255,
            Math.floor(
                b +
                (255 - b) *
                cantidad
            )
        );


    return (
        "#" +
        [r, g, b]
            .map(
                x =>
                    x
                        .toString(16)
                        .padStart(
                            2,
                            "0"
                        )
            )
            .join("")
    );

}


// ============================================================
// COPIAR OBJETO
// ============================================================

function copiarObjeto(
    objeto
) {

    return JSON.parse(
        JSON.stringify(
            objeto
        )
    );

}


// ============================================================
// SEGURIDAD HTML
// ============================================================

function escaparHtml(
    valor
) {

    return String(
        valor || ""
    )
        .replace(
            /&/g,
            "&amp;"
        )
        .replace(
            /</g,
            "&lt;"
        )
        .replace(
            />/g,
            "&gt;"
        )
        .replace(
            /"/g,
            "&quot;"
        )
        .replace(
            /'/g,
            "&#039;"
        );

}// ============================================================
// CONFIGURACIÓN MÉTODOS DE PAGO
// ============================================================

function iniciarConfiguracionPagos() {

    const abrir =
        document.getElementById(
            "abrirConfigPagos"
        );

    const cerrar =
        document.getElementById(
            "cerrarConfigPagos"
        );

    const panel =
        document.getElementById(
            "configPagos"
        );

    const overlay =
        document.getElementById(
            "configPagosOverlay"
        );


    if (
        !panel ||
        !overlay
    ) {
        return;
    }


    function abrirPanel() {

        panel.hidden =
            false;

        overlay.hidden =
            false;


        requestAnimationFrame(
            function () {

                panel.classList.add(
                    "visible"
                );

            }
        );

    }


    function cerrarPanel() {

        panel.classList.remove(
            "visible"
        );

        panel.hidden =
            true;

        overlay.hidden =
            true;

    }


    if (abrir) {

        abrir.addEventListener(
            "click",
            abrirPanel
        );

    }


    if (cerrar) {

        cerrar.addEventListener(
            "click",
            cerrarPanel
        );

    }


    overlay.addEventListener(
        "click",
        cerrarPanel
    );


    document.addEventListener(
        "keydown",
        function (event) {

            if (
                event.key ===
                "Escape"
            ) {

                cerrarPanel();

            }

        }
    );


    const guardar =
        document.getElementById(
            "guardarConfigPagos"
        );


    if (guardar) {

        guardar.addEventListener(
            "click",
            function () {

                actualizarConfiguracionPagos();

                cerrarPanel();


                if (
                    typeof UIkit !==
                    "undefined"
                ) {

                    UIkit.notification({

                        message:
                            "Configuración guardada correctamente",

                        status:
                            "success",

                        pos:
                            "top-right",

                        timeout:
                            1800

                    });

                }

            }
        );

    }


    const restablecer =
        document.getElementById(
            "restablecerConfigPagos"
        );


    if (restablecer) {

        restablecer.addEventListener(
            "click",
            function () {

                configuracionKernix.pagos =
                    copiarObjeto(
                        KERNIX_CONFIG_DEFAULT.pagos
                    );


                guardarConfiguracionKernix();

                cargarInputsConfiguracion();

                actualizarValorProfundidadPagos();

                reconstruirGraficoPagos();


                renderizarLeyendaPagos(
                    window.kernixVentasData
                        ?.metodosPago || []
                );

            }
        );

    }


    configurarCambiosPagosTiempoReal();

}


// ============================================================
// CAMBIO EN TIEMPO REAL
// ============================================================

function configurarCambiosPagosTiempoReal() {

    const ids = [

        "colorYape",
        "colorPlin",
        "colorEfectivo",
        "colorTarjeta",
        "colorTransferencia",
        "pagosMin",
        "pagosMax",
        "pagosProfundidad"

    ];


    ids.forEach(
        function (id) {

            const input =
                document.getElementById(
                    id
                );


            if (!input) {
                return;
            }


            input.addEventListener(
                "input",
                function () {

                    actualizarConfiguracionPagos(
                        false
                    );

                }
            );

        }
    );

}


// ============================================================
// ACTUALIZAR CONFIGURACIÓN
// ============================================================

function actualizarConfiguracionPagos(
    guardar = true
) {

    configuracionKernix.pagos.yape =
        obtenerValor(
            "colorYape",
            "#8e35ff"
        );


    configuracionKernix.pagos.plin =
        obtenerValor(
            "colorPlin",
            "#00d9a5"
        );


    configuracionKernix.pagos.efectivo =
        obtenerValor(
            "colorEfectivo",
            "#f8c23d"
        );


    configuracionKernix.pagos.tarjeta =
        obtenerValor(
            "colorTarjeta",
            "#1398ff"
        );


    configuracionKernix.pagos.transferencia =
        obtenerValor(
            "colorTransferencia",
            "#15d8ef"
        );


    configuracionKernix.pagos.min =
        obtenerNumero(
            "pagosMin",
            0
        );


    configuracionKernix.pagos.max =
        obtenerNumeroNullable(
            "pagosMax"
        );


    configuracionKernix.pagos.profundidad =
        obtenerNumero(
            "pagosProfundidad",
            11
        );


    if (guardar) {

        guardarConfiguracionKernix();

    }


    actualizarValorProfundidadPagos();


    reconstruirGraficoPagos();


    renderizarLeyendaPagos(
        window.kernixVentasData
            ?.metodosPago || []
    );

}


// ============================================================
// VALOR PROFUNDIDAD
// ============================================================

function actualizarValorProfundidadPagos() {

    const valor =
        document.getElementById(
            "pagosProfundidadValor"
        );


    if (!valor) {
        return;
    }


    valor.textContent =
        configuracionKernix
            .pagos
            .profundidad;

}


// ============================================================
// BUSCADOR MÉTODO DE PAGO
// ============================================================

function iniciarBuscadorMetodosPago() {

    const input =
        document.getElementById(
            "buscarMetodoPago"
        );

    const limpiar =
        document.getElementById(
            "limpiarBusquedaPago"
        );


    if (!input) {
        return;
    }


    input.addEventListener(
        "input",
        function () {

            const termino =
                normalizarTextoBusqueda(
                    input.value
                );


            if (limpiar) {

                limpiar.classList.toggle(
                    "visible",
                    termino.length > 0
                );

            }


            filtrarMetodosPago(
                termino
            );

        }
    );


    if (limpiar) {

        limpiar.addEventListener(
            "click",
            function () {

                input.value =
                    "";

                limpiar.classList.remove(
                    "visible"
                );


                filtrarMetodosPago(
                    ""
                );


                input.focus();

            }
        );

    }

}


// ============================================================
// FILTRAR DATOS
// ============================================================

function filtrarMetodosPago(
    termino
) {

    const originales =
        window.kernixVentasData
            ?.metodosPago || [];


    const filtrados =
        termino === ""
            ? originales
            : originales.filter(
                function (item) {

                    return normalizarTextoBusqueda(
                        item.metodo
                    )
                        .includes(
                            termino
                        );

                }
            );


    if (graficoPagos) {

        graficoPagos.destroy();

        graficoPagos =
            null;

    }


    crearGraficoPagos3D(
        filtrados
    );


    renderizarLeyendaPagos(
        filtrados
    );

}


// ============================================================
// NORMALIZAR TEXTO
// ============================================================

function normalizarTextoBusqueda(
    valor
) {

    return String(
        valor || ""
    )
        .toLowerCase()
        .normalize(
            "NFD"
        )
        .replace(
            /[\u0300-\u036f]/g,
            ""
        )
        .trim();

}// ============================================================
// KERNIX
// BUSCADORES INTERNOS DEL DASHBOARD
// ============================================================

document.addEventListener(
    "DOMContentLoaded",
    function () {

        iniciarBuscadoresDashboard();

    }
);


// ============================================================
// INICIAR BUSCADORES
// ============================================================

function iniciarBuscadoresDashboard() {

    configurarBuscadorSeccion({

        inputId:
            "buscarTopProductos",

        selector:
            ".kxitembuscable",

        vacioId:
            "sinResultadosTopProductos"

    });
    configurarBuscadorSeccion({

        inputId:
            "buscarClientesDestacados",

        selector:
            ".kxclientebuscable",

        vacioId:
            "sinResultadosClientes"

    });

    configurarBuscadorSeccion({

        inputId:
            "buscarUltimasVentas",

        selector:
            ".kxventabuscable",

        vacioId:
            "sinResultadosUltimasVentas"

    });


    configurarBuscadorSeccion({

        inputId:
            "buscarActividad",

        selector:
            ".kxactividadbuscable",

        vacioId:
            "sinResultadosActividad"

    });


    configurarBuscadorSeccion({

        inputId:
            "buscarIa",

        selector:
            ".kxiabuscable",

        vacioId:
            "sinResultadosIa"

    });


    configurarBotonesLimpiarBusqueda();

}


// ============================================================
// CONFIGURAR UN BUSCADOR
// ============================================================

function configurarBuscadorSeccion(
    opciones
) {

    const input =
        document.getElementById(
            opciones.inputId
        );


    if (!input) {
        return;
    }


    const vacio =
        document.getElementById(
            opciones.vacioId
        );


    input.addEventListener(
        "input",
        function () {

            ejecutarBusquedaSeccion(
                input,
                opciones.selector,
                vacio
            );

        }
    );


    input.addEventListener(
        "keydown",
        function (event) {

            if (
                event.key ===
                "Escape"
            ) {

                input.value =
                    "";

                ejecutarBusquedaSeccion(
                    input,
                    opciones.selector,
                    vacio
                );

                input.blur();

            }

        }
    );

}


// ============================================================
// EJECUTAR BÚSQUEDA
// ============================================================

function ejecutarBusquedaSeccion(
    input,
    selector,
    vacio
) {

    const termino =
        normalizarBusqueda(
            input.value
        );


    const elementos =
        Array.from(
            document.querySelectorAll(
                selector
            )
        );


    let encontrados =
        0;


    elementos.forEach(
        function (elemento) {

            const contenido =
                normalizarBusqueda(
                    elemento.dataset.search ||
                    elemento.textContent ||
                    ""
                );


            const coincide =
                termino.length === 0 ||
                contenido.includes(
                    termino
                );


            elemento.hidden =
                !coincide;


            if (coincide) {

                encontrados++;

            }

        }
    );


    if (vacio) {

        vacio.hidden =
            termino.length === 0 ||
            encontrados > 0;

    }


    actualizarBotonLimpiar(
        input
    );

}


// ============================================================
// BOTONES LIMPIAR
// ============================================================

function configurarBotonesLimpiarBusqueda() {

    document
        .querySelectorAll(
            ".kxlimpiarbusqueda"
        )
        .forEach(
            function (boton) {

                const target =
                    boton.dataset.target;


                const input =
                    document.getElementById(
                        target
                    );


                if (!input) {
                    return;
                }


                boton.addEventListener(
                    "click",
                    function () {

                        input.value =
                            "";

                        input.dispatchEvent(
                            new Event(
                                "input",
                                {
                                    bubbles:
                                        true
                                }
                            )
                        );

                        input.focus();

                    }
                );


                actualizarBotonLimpiar(
                    input
                );

            }
        );

}


// ============================================================
// MOSTRAR / OCULTAR X
// ============================================================

function actualizarBotonLimpiar(
    input
) {

    const boton =
        document.querySelector(
            `.kxlimpiarbusqueda[data-target="${input.id}"]`
        );


    if (!boton) {
        return;
    }


    boton.classList.toggle(
        "visible",
        input.value.trim().length > 0
    );

}


// ============================================================
// NORMALIZAR TEXTO
// ============================================================

function normalizarBusqueda(
    valor
) {

    return String(
        valor || ""
    )
    .toLowerCase()
    .normalize(
        "NFD"
    )
    .replace(
        /[\u0300-\u036f]/g,
        ""
    )
    .replace(
        /\s+/g,
        " "
    )
    .trim();

}let graficoProyeccion = null;

document.addEventListener(
    "DOMContentLoaded",
    function () {

        crearGraficoProyeccion();

    }
);


function crearGraficoProyeccion() {

    const canvas =
        document.getElementById(
            "proyeccionChart"
        );

    if (!canvas) {
        return;
    }


    const datos =
        window.kernixVentasData
            ?.proyeccionVentas || [];


    if (datos.length === 0) {
        return;
    }


    if (graficoProyeccion) {

        graficoProyeccion.destroy();

    }


    const contexto =
        canvas.getContext("2d");


    const gradiente =
        contexto.createLinearGradient(
            0,
            0,
            0,
            320
        );


    gradiente.addColorStop(
        0,
        "rgba(166,108,255,.32)"
    );

    gradiente.addColorStop(
        .55,
        "rgba(50,167,255,.10)"
    );

    gradiente.addColorStop(
        1,
        "rgba(50,167,255,0)"
    );


    graficoProyeccion =
        new Chart(
            canvas,
            {

                type:
                    "line",

                data: {

                    labels:
                        datos.map(
                            item =>
                                item.etiqueta
                        ),

                    datasets: [

                        {
                            label:
                                "Máximo",

                            data:
                                datos.map(
                                    item =>
                                        Number(
                                            item.maximo || 0
                                        )
                                ),

                            borderColor:
                                "rgba(166,108,255,.28)",

                            borderWidth:
                                1,

                            borderDash:
                                [5,5],

                            pointRadius:
                                0,

                            tension:
                                .4
                        },


                        {
                            label:
                                "Proyección",

                            data:
                                datos.map(
                                    item =>
                                        Number(
                                            item.estimado || 0
                                        )
                                ),

                            borderColor:
                                "#a66cff",

                            backgroundColor:
                                gradiente,

                            borderWidth:
                                3,

                            fill:
                                true,

                            tension:
                                .42,

                            pointRadius:
                                4,

                            pointHoverRadius:
                                7,

                            pointBackgroundColor:
                                "#f8c95c",

                            pointBorderColor:
                                "#07101d",

                            pointBorderWidth:
                                2
                        },


                        {
                            label:
                                "Mínimo",

                            data:
                                datos.map(
                                    item =>
                                        Number(
                                            item.minimo || 0
                                        )
                                ),

                            borderColor:
                                "rgba(36,230,255,.24)",

                            borderWidth:
                                1,

                            borderDash:
                                [5,5],

                            pointRadius:
                                0,

                            tension:
                                .4
                        }

                    ]

                },


                options: {

                    responsive:
                        true,

                    maintainAspectRatio:
                        false,


                    interaction: {

                        mode:
                            "index",

                        intersect:
                            false

                    },


                    plugins: {

                        legend: {

                            position:
                                "top",

                            align:
                                "end",

                            labels: {

                                color:
                                    "#71879d",

                                usePointStyle:
                                    true,

                                pointStyle:
                                    "circle",

                                boxWidth:
                                    7,

                                padding:
                                    14

                            }

                        },


                        tooltip: {

                            backgroundColor:
                                "rgba(4,12,23,.97)",

                            borderColor:
                                "rgba(166,108,255,.25)",

                            borderWidth:
                                1,

                            titleColor:
                                "#ffffff",

                            bodyColor:
                                "#c7d6e5",

                            padding:
                                11,


                            callbacks: {

                                label:
                                    function (
                                        context
                                    ) {

                                        return (
                                            context.dataset.label +
                                            ": S/ " +
                                            Number(
                                                context.raw
                                            )
                                            .toLocaleString(
                                                "es-PE",
                                                {
                                                    minimumFractionDigits:
                                                        2,

                                                    maximumFractionDigits:
                                                        2
                                                }
                                            )
                                        );

                                    }

                            }

                        }

                    },


                    scales: {

                        x: {

                            border: {
                                display: false
                            },

                            grid: {
                                display: false
                            },

                            ticks: {

                                color:
                                    "#60758b",

                                font: {
                                    size: 9
                                }

                            }

                        },


                        y: {

                            beginAtZero:
                                true,

                            border: {
                                display: false
                            },

                            grid: {

                                color:
                                    "rgba(100,145,190,.07)"

                            },

                            ticks: {

                                color:
                                    "#60758b",

                                font: {
                                    size: 9
                                },

                                callback:
                                    function (
                                        value
                                    ) {

                                        if (
                                            value >= 1000
                                        ) {

                                            return (
                                                "S/ " +
                                                (
                                                    value /
                                                    1000
                                                )
                                                .toFixed(
                                                    1
                                                ) +
                                                "K"
                                            );

                                        }

                                        return (
                                            "S/ " +
                                            value
                                        );

                                    }

                            }

                        }

                    }

                }

            }
        );

}