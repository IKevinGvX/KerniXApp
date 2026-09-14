document.addEventListener(
    "DOMContentLoaded",
    function () {

        iniciarBuscadorClientes();

    }
);


/* ==========================================================
   CLIENTES
   ========================================================== */

function iniciarBuscadorClientes() {

    const input =
        document.getElementById(
            "buscarCliente"
        );

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );

    const quitarCliente =
        document.getElementById(
            "quitarCliente"
        );


    if (!input) {

        console.warn(
            "No existe #buscarCliente"
        );

        return;

    }


    if (!resultados) {

        console.warn(
            "No existe #resultadosClientes"
        );

        return;

    }


    let temporizadorBusqueda =
        null;


    input.addEventListener(
        "input",
        function () {

            clearTimeout(
                temporizadorBusqueda
            );


            const termino =
                input.value.trim();


            if (
                termino.length < 2
            ) {

                cerrarResultadosClientes();

                return;

            }


            temporizadorBusqueda =
                setTimeout(
                    function () {

                        buscarClientes(
                            termino
                        );

                    },
                    300
                );

        }
    );


    input.addEventListener(
        "focus",
        function () {

            const termino =
                input.value.trim();


            if (
                termino.length >= 2
            ) {

                buscarClientes(
                    termino
                );

            }

        }
    );


    input.addEventListener(
        "keydown",
        function (event) {

            if (
                event.key === "Escape"
            ) {

                cerrarResultadosClientes();

                input.blur();

            }

        }
    );


    if (quitarCliente) {

        quitarCliente.addEventListener(
            "click",
            function () {

                limpiarClienteSeleccionado();

            }
        );

    }


    document.addEventListener(
        "click",
        function (event) {

            const contenedor =
                document.querySelector(
                    ".kxclientebuscador"
                );


            if (
                contenedor &&
                !contenedor.contains(
                    event.target
                )
            ) {

                cerrarResultadosClientes();

            }

        }
    );
}


/* ==========================================================
   BUSCAR CLIENTES
   ========================================================== */

async function buscarClientes(
    termino
) {

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );


    if (!resultados) {

        return;

    }


    resultados.hidden =
        false;


    resultados.innerHTML = `
        <div class="kxclientecargando">
            Buscando clientes...
        </div>
    `;


    try {

        const url =
            `${window.location.pathname}?handler=BuscarClientes&termino=${encodeURIComponent(
                termino
            )}`;


        console.log(
            "Buscando clientes:",
            url
        );


        const response =
            await fetch(
                url,
                {
                    method:
                        "GET",

                    headers: {

                        "Accept":
                            "application/json"

                    },

                    credentials:
                        "same-origin"
                }
            );


        if (!response.ok) {

            throw new Error(
                `HTTP ${response.status}`
            );

        }


        const resultado =
            await response.json();


        console.log(
            "Respuesta clientes:",
            resultado
        );


        if (
            resultado.success !== true
        ) {

            throw new Error(
                resultado.message ||
                "No se pudieron cargar los clientes."
            );

        }


        const clientes =
            Array.isArray(
                resultado.data
            )
                ? resultado.data
                : [];


        mostrarClientes(
            clientes
        );

    }
    catch (error) {

        console.error(
            "Error buscando clientes:",
            error
        );


        resultados.innerHTML = `
            <div class="kxclienteerror">
                No fue posible cargar los clientes.
            </div>
        `;

    }
}


/* ==========================================================
   MOSTRAR RESULTADOS
   ========================================================== */

function mostrarClientes(
    clientes
) {

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );


    if (!resultados) {

        return;

    }


    resultados.innerHTML =
        "";


    if (
        clientes.length === 0
    ) {

        resultados.innerHTML = `
            <div class="kxclientesinvacio">

                No se encontraron clientes.

            </div>
        `;

        return;

    }


    clientes.forEach(
        function (cliente) {

            const nombre =
                obtenerNombreCliente(
                    cliente
                );


            const documento =
                obtenerDocumentoCliente(
                    cliente
                );


            const boton =
                document.createElement(
                    "button"
                );


            boton.type =
                "button";


            boton.className =
                "kxresultadocliente";


            boton.innerHTML = `

                <div class="kxresultadoavatar">

                    ${escaparHtml(
                obtenerInicial(
                    nombre
                )
            )}

                </div>


                <div class="kxresultadoinfo">

                    <strong>

                        ${escaparHtml(
                nombre
            )}

                    </strong>


                    <span>

                        ${escaparHtml(
                documento
            )}

                    </span>


                    <small>

                        ${escaparHtml(
                cliente.correo ||
                cliente.telefono ||
                ""
            )}

                    </small>

                </div>

            `;


            boton.addEventListener(
                "click",
                function () {

                    seleccionarCliente(
                        cliente
                    );

                }
            );


            resultados.appendChild(
                boton
            );

        }
    );
}


/* ==========================================================
   SELECCIONAR CLIENTE
   ========================================================== */

function seleccionarCliente(
    cliente
) {

    const nombre =
        obtenerNombreCliente(
            cliente
        );


    const documento =
        obtenerDocumentoCliente(
            cliente
        );


    const idCliente =
        document.getElementById(
            "idClienteSeleccionado"
        );


    const buscarCliente =
        document.getElementById(
            "buscarCliente"
        );


    const documentoCliente =
        document.getElementById(
            "documentoCliente"
        );


    const telefonoCliente =
        document.getElementById(
            "telefonoCliente"
        );


    const clienteSeleccionado =
        document.getElementById(
            "clienteSeleccionado"
        );


    const clienteNombre =
        document.getElementById(
            "clienteNombre"
        );


    const clienteDocumentoTexto =
        document.getElementById(
            "clienteDocumentoTexto"
        );


    const clienteCorreo =
        document.getElementById(
            "clienteCorreo"
        );


    const clienteAvatar =
        document.getElementById(
            "clienteAvatar"
        );


    if (idCliente) {

        idCliente.value =
            cliente.idCliente || "";

    }


    if (buscarCliente) {

        buscarCliente.value =
            nombre;

    }


    if (documentoCliente) {

        documentoCliente.value =
            cliente.numeroDocumento || "";

    }


    if (telefonoCliente) {

        telefonoCliente.value =
            cliente.telefono || "";

    }


    if (clienteNombre) {

        clienteNombre.textContent =
            nombre;

    }


    if (clienteDocumentoTexto) {

        clienteDocumentoTexto.textContent =
            documento;

    }


    if (clienteCorreo) {

        clienteCorreo.textContent =
            cliente.correo || "";

    }


    if (clienteAvatar) {

        clienteAvatar.textContent =
            obtenerInicial(
                nombre
            );

    }


    if (clienteSeleccionado) {

        clienteSeleccionado.hidden =
            false;

    }


    cerrarResultadosClientes();


    console.log(
        "Cliente seleccionado:",
        cliente
    );
}


/* ==========================================================
   LIMPIAR CLIENTE
   ========================================================== */

function limpiarClienteSeleccionado() {

    const controles =
        [
            "idClienteSeleccionado",
            "buscarCliente",
            "documentoCliente",
            "telefonoCliente"
        ];


    controles.forEach(
        function (id) {

            const elemento =
                document.getElementById(
                    id
                );


            if (elemento) {

                elemento.value =
                    "";

            }

        }
    );


    const nombre =
        document.getElementById(
            "clienteNombre"
        );


    if (nombre) {

        nombre.textContent =
            "";

    }


    const documento =
        document.getElementById(
            "clienteDocumentoTexto"
        );


    if (documento) {

        documento.textContent =
            "";

    }


    const correo =
        document.getElementById(
            "clienteCorreo"
        );


    if (correo) {

        correo.textContent =
            "";

    }


    const seleccionado =
        document.getElementById(
            "clienteSeleccionado"
        );


    if (seleccionado) {

        seleccionado.hidden =
            true;

    }


    cerrarResultadosClientes();


    const input =
        document.getElementById(
            "buscarCliente"
        );


    if (input) {

        input.focus();

    }
}


/* ==========================================================
   CERRAR RESULTADOS
   ========================================================== */

function cerrarResultadosClientes() {

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );


    if (!resultados) {

        return;

    }


    resultados.hidden =
        true;


    resultados.innerHTML =
        "";
}


/* ==========================================================
   HELPERS
   ========================================================== */

function obtenerNombreCliente(
    cliente
) {

    if (
        cliente.nombreMostrar
    ) {

        return cliente.nombreMostrar;

    }


    if (
        cliente.nombreCompleto
    ) {

        return cliente.nombreCompleto;

    }


    if (
        cliente.razonSocial
    ) {

        return cliente.razonSocial;

    }


    const nombres =
        cliente.nombres || "";


    const apellidos =
        cliente.apellidos || "";


    const completo =
        `${nombres} ${apellidos}`
            .trim();


    return completo || "Cliente";
}


function obtenerDocumentoCliente(
    cliente
) {

    const tipo =
        cliente.tipoDocumento || "";


    const numero =
        cliente.numeroDocumento || "";


    return `${tipo} ${numero}`
        .trim();
}


function obtenerInicial(
    valor
) {

    if (
        !valor
    ) {

        return "C";

    }


    return valor
        .trim()
        .charAt(0)
        .toUpperCase();
}


function escaparHtml(
    valor
) {

    const elemento =
        document.createElement(
            "div"
        );


    elemento.textContent =
        String(
            valor || ""
        );


    return elemento.innerHTML;
}/* ==========================================================
   PRODUCTOS REALES
   ========================================================== */

let temporizadorProductos = null;


document.addEventListener(
    "DOMContentLoaded",
    function () {

        iniciarBuscadorProductos();

    }
);


function iniciarBuscadorProductos() {

    const input =
        document.getElementById(
            "buscarProducto"
        );


    if (!input) {
        return;
    }


    input.addEventListener(
        "input",
        function () {

            clearTimeout(
                temporizadorProductos
            );


            const termino =
                input.value.trim();


            if (termino.length < 2) {

                mostrarMensajeProductos(
                    "Escribe al menos 2 caracteres para buscar productos."
                );

                return;
            }


            temporizadorProductos =
                setTimeout(
                    function () {

                        buscarProductos(
                            termino
                        );

                    },
                    300
                );

        }
    );
}


async function buscarProductos(
    termino
) {

    mostrarMensajeProductos(
        "Buscando productos..."
    );


    try {

        const url =
            `${window.location.pathname}?handler=BuscarProductos&termino=${encodeURIComponent(
                termino
            )}`;


        const response =
            await fetch(
                url,
                {
                    method:
                        "GET",

                    headers: {
                        "Accept":
                            "application/json"
                    },

                    credentials:
                        "same-origin"
                }
            );


        if (!response.ok) {

            throw new Error(
                `HTTP ${response.status}`
            );

        }


        const resultado =
            await response.json();


        if (!resultado.success) {

            throw new Error(
                resultado.message ||
                "No se pudieron cargar los productos."
            );

        }


        mostrarProductos(
            resultado.data || []
        );

    }
    catch (error) {

        console.error(
            "Error buscando productos:",
            error
        );


        mostrarMensajeProductos(
            "No fue posible cargar productos."
        );

    }
}


function mostrarProductos(
    productos
) {

    const contenedor =
        document.getElementById(
            "productosDisponibles"
        );


    if (!contenedor) {
        return;
    }


    contenedor.innerHTML =
        "";


    if (productos.length === 0) {

        mostrarMensajeProductos(
            "No se encontraron productos."
        );

        return;
    }


    productos.forEach(
        function (producto) {

            const card =
                document.createElement(
                    "button"
                );


            card.type =
                "button";


            card.className =
                "kxproductocard";


            const sinStock =
                producto.controlaStock === true
                &&
                Number(
                    producto.stockDisponible
                ) <= 0;


            if (sinStock) {

                card.classList.add(
                    "sinstock"
                );

            }


            card.innerHTML = `

                <div class="kxproductoicono">

                    <span uk-icon="icon: cart"></span>

                </div>


                <div class="kxproductoinfo">

                    <strong>
                        ${escaparHtml(
                producto.nombre
            )}
                    </strong>

                    <span>
                        ${escaparHtml(
                producto.codigo
            )}
                    </span>

                    <small>

                        ${escaparHtml(
                producto.categoria || ""
            )}

                        ·

                        Stock:
                        ${Number(
                producto.stockDisponible || 0
            )}

                        ${escaparHtml(
                producto.abreviaturaUnidad ||
                ""
            )}

                    </small>

                </div>


                <div class="kxproductoprecio">

                    S/
                    ${Number(
                producto.precioVenta || 0
            ).toFixed(2)}

                </div>

            `;


            if (!sinStock) {

                card.addEventListener(
                    "click",
                    function () {

                        agregarProductoCarrito(
                            producto
                        );

                    }
                );

            }


            contenedor.appendChild(
                card
            );

        }
    );


    if (window.UIkit) {

        UIkit.icon(
            contenedor
        );

    }
}


function mostrarMensajeProductos(
    mensaje
) {

    const contenedor =
        document.getElementById(
            "productosDisponibles"
        );


    if (!contenedor) {
        return;
    }


    contenedor.innerHTML = `

        <div class="kxvacioproductos">

            ${escaparHtml(
        mensaje
    )}

        </div>

    `;
}/* ==========================================================
   CARRITO DE NUEVA VENTA
   ========================================================== */

const carritoVenta = [];

const CONFIG_VENTA = {

    descuentoMaximoPorcentaje:
        Number(
            window.kernixConfiguracionVenta
                ?.descuentoMaximo ?? 0
        ),

    permitirVentaSinStock:
        window.kernixConfiguracionVenta
            ?.permitirVentaSinStock === true,

    validarStockAntesVenta:
        window.kernixConfiguracionVenta
            ?.validarStockAntesVenta !== false,

    permitirCambioPrecio:
        window.kernixConfiguracionVenta
            ?.permitirCambioPrecio === true,

    permitirVentaCredito:
        window.kernixConfiguracionVenta
            ?.permitirVentaCredito === true,

    idEmpresa:
        Number(
            window.kernixConfiguracionVenta
                ?.idEmpresa ?? 0
        ),

    idSucursal:
        Number(
            window.kernixConfiguracionVenta
                ?.idSucursal ?? 0
        ),

    idAlmacen:
        Number(
            window.kernixConfiguracionVenta
                ?.idAlmacen ?? 0
        )
};


console.log(
    "[KerniX] Reglas activas:",
    CONFIG_VENTA
);


/* ==========================================================
   AGREGAR PRODUCTO
   ========================================================== */

function agregarProductoCarrito(producto) {

    if (!producto) {
        return;
    }


    const idProducto =
        Number(producto.idProducto);


    if (!idProducto) {
        return;
    }


    const stockDisponible =
        Number(
            producto.stockDisponible || 0
        );


    const controlaStock =
        producto.controlaStock === true;


    if (
        controlaStock &&
        stockDisponible <= 0
    ) {

        mostrarAvisoVenta(
            "El producto no tiene stock disponible.",
            "warning"
        );

        return;
    }


    const existente =
        carritoVenta.find(
            item =>
                item.idProducto === idProducto
        );


    if (existente) {

        const nuevaCantidad =
            existente.cantidad + 1;


        if (
            controlaStock &&
            nuevaCantidad > stockDisponible
        ) {

            mostrarAvisoVenta(
                `Solo hay ${stockDisponible} ${producto.abreviaturaUnidad || "und"} disponibles.`,
                "warning"
            );

            return;
        }


        existente.cantidad =
            nuevaCantidad;

    }
    else {

        carritoVenta.push({

            idProducto:
                idProducto,

            codigo:
                producto.codigo || "",

            nombre:
                producto.nombre || "",

            precioUnitario:
                Number(
                    producto.precioVenta || 0
                ),

            cantidad:
                1,

            descuentoPorcentaje:
                0,

            stockDisponible:
                stockDisponible,

            controlaStock:
                controlaStock,

            abreviaturaUnidad:
                producto.abreviaturaUnidad || "und"

        });

    }


    renderizarCarrito();

    mostrarAvisoVenta(
        `${producto.nombre} agregado a la venta.`,
        "success"
    );
}


/* ==========================================================
   RENDERIZAR
   ========================================================== */

function renderizarCarrito() {

    const tbody =
        document.getElementById(
            "detalleVenta"
        );


    if (!tbody) {
        return;
    }


    tbody.innerHTML =
        "";


    if (
        carritoVenta.length === 0
    ) {

        tbody.innerHTML = `

            <tr id="filaVacia">

                <td colspan="6"
                    class="kxvacio">

                    No has agregado productos a la venta.

                </td>

            </tr>

        `;


        actualizarContadorProductos();

        recalcularVenta();

        return;
    }


    carritoVenta.forEach(
        function (item) {

            const subtotalLinea =
                item.precioUnitario *
                item.cantidad;


            const descuentoMonto =
                subtotalLinea *
                (
                    item.descuentoPorcentaje /
                    100
                );


            const totalLinea =
                subtotalLinea -
                descuentoMonto;


            const tr =
                document.createElement(
                    "tr"
                );


            tr.innerHTML = `

                <td>

                    <div class="kxcarritoproducto">

                        <strong>
                            ${escaparHtml(
                item.nombre
            )}
                        </strong>

                        <span>
                            ${escaparHtml(
                item.codigo
            )}
                        </span>

                    </div>

                </td>


                <td>

                    <strong>
                        S/
                        ${formatearNumeroVenta(
                item.precioUnitario
            )}
                    </strong>

                </td>


                <td>

                    <div class="kxcantidadcontrol">

                        <button type="button"
                                class="kxcantidadboton"
                                data-accion="restar"
                                data-id="${item.idProducto}">
                            −
                        </button>


                        <input type="number"
                               class="kxcantidadinput"
                               data-id="${item.idProducto}"
                               min="1"
                               step="1"
                               value="${item.cantidad}" />


                        <button type="button"
                                class="kxcantidadboton"
                                data-accion="sumar"
                                data-id="${item.idProducto}">
                            +
                        </button>

                    </div>


                    <small class="kxstockcarrito">

                        Disponible:
                        ${item.stockDisponible}
                        ${escaparHtml(
                item.abreviaturaUnidad
            )}

                    </small>

                </td>


                <td>

                    <div class="kxdescuentocontrol">

                        <input type="number"
                               class="kxdescuentoinput"
                               data-id="${item.idProducto}"
                               min="0"
                               max="${CONFIG_VENTA.descuentoMaximoPorcentaje}"
                               step="0.01"
                               value="${item.descuentoPorcentaje}" />

                        <span>
                            %
                        </span>

                    </div>


                    <small class="kxdescuentolimite">

                        Máx.
                        ${CONFIG_VENTA.descuentoMaximoPorcentaje}%

                    </small>

                </td>


                <td>

                    <strong class="kxtotallinea">

                        S/
                        ${formatearNumeroVenta(
                totalLinea
            )}

                    </strong>

                </td>


                <td>

                    <button type="button"
                            class="kxquitarproducto"
                            data-id="${item.idProducto}"
                            title="Quitar producto">

                        <span uk-icon="icon: trash"></span>

                    </button>

                </td>

            `;


            tbody.appendChild(
                tr
            );

        }
    );


    registrarEventosCarrito();

    actualizarContadorProductos();

    recalcularVenta();


    if (window.UIkit) {

        UIkit.icon(
            tbody
        );

    }
}


/* ==========================================================
   EVENTOS DEL CARRITO
   ========================================================== */

function registrarEventosCarrito() {

    document
        .querySelectorAll(
            ".kxcantidadboton"
        )
        .forEach(
            function (boton) {

                boton.addEventListener(
                    "click",
                    function () {

                        const idProducto =
                            Number(
                                boton.dataset.id
                            );


                        const accion =
                            boton.dataset.accion;


                        cambiarCantidadProducto(
                            idProducto,
                            accion
                        );

                    }
                );

            }
        );


    document
        .querySelectorAll(
            ".kxcantidadinput"
        )
        .forEach(
            function (input) {

                input.addEventListener(
                    "change",
                    function () {

                        const idProducto =
                            Number(
                                input.dataset.id
                            );


                        const cantidad =
                            Number(
                                input.value
                            );


                        establecerCantidadProducto(
                            idProducto,
                            cantidad
                        );

                    }
                );

            }
        );


    document
        .querySelectorAll(
            ".kxdescuentoinput"
        )
        .forEach(
            function (input) {

                input.addEventListener(
                    "change",
                    function () {

                        const idProducto =
                            Number(
                                input.dataset.id
                            );


                        const descuento =
                            Number(
                                input.value
                            );


                        establecerDescuentoProducto(
                            idProducto,
                            descuento
                        );

                    }
                );

            }
        );


    document
        .querySelectorAll(
            ".kxquitarproducto"
        )
        .forEach(
            function (boton) {

                boton.addEventListener(
                    "click",
                    function () {

                        const idProducto =
                            Number(
                                boton.dataset.id
                            );


                        quitarProductoCarrito(
                            idProducto
                        );

                    }
                );

            }
        );
}


/* ==========================================================
   CANTIDAD
   ========================================================== */

function cambiarCantidadProducto(
    idProducto,
    accion
) {

    const item =
        carritoVenta.find(
            x =>
                x.idProducto === idProducto
        );


    if (!item) {
        return;
    }


    let nuevaCantidad =
        item.cantidad;


    if (
        accion === "sumar"
    ) {

        nuevaCantidad++;

    }


    if (
        accion === "restar"
    ) {

        nuevaCantidad--;

    }


    establecerCantidadProducto(
        idProducto,
        nuevaCantidad
    );
}


function establecerCantidadProducto(
    idProducto,
    cantidad
) {

    const item =
        carritoVenta.find(
            x =>
                x.idProducto === idProducto
        );


    if (!item) {
        return;
    }


    let nuevaCantidad =
        Math.floor(
            Number(cantidad)
        );


    if (
        !Number.isFinite(
            nuevaCantidad
        )
    ) {

        nuevaCantidad =
            1;

    }


    if (
        nuevaCantidad < 1
    ) {

        nuevaCantidad =
            1;

    }


    if (
        item.controlaStock &&
        nuevaCantidad >
        item.stockDisponible
    ) {

        mostrarAvisoVenta(
            `No puedes vender ${nuevaCantidad}. Stock disponible: ${item.stockDisponible}.`,
            "warning"
        );


        nuevaCantidad =
            item.stockDisponible;

    }


    item.cantidad =
        nuevaCantidad;


    renderizarCarrito();
}


/* ==========================================================
   DESCUENTO
   ========================================================== */

function establecerDescuentoProducto(
    idProducto,
    descuento
) {

    const item =
        carritoVenta.find(
            x =>
                x.idProducto === idProducto
        );


    if (!item) {
        return;
    }


    let valor =
        Number(
            descuento
        );


    if (
        !Number.isFinite(
            valor
        )
    ) {

        valor =
            0;

    }


    if (
        valor < 0
    ) {

        valor =
            0;

    }


    if (
        valor >
        CONFIG_VENTA
            .descuentoMaximoPorcentaje
    ) {

        mostrarAvisoVenta(
            `El descuento máximo permitido es ${CONFIG_VENTA.descuentoMaximoPorcentaje}%.`,
            "warning"
        );


        valor =
            CONFIG_VENTA
                .descuentoMaximoPorcentaje;

    }


    item.descuentoPorcentaje =
        valor;


    renderizarCarrito();
}


/* ==========================================================
   QUITAR
   ========================================================== */

function quitarProductoCarrito(
    idProducto
) {

    const indice =
        carritoVenta.findIndex(
            x =>
                x.idProducto === idProducto
        );


    if (
        indice === -1
    ) {
        return;
    }


    const nombre =
        carritoVenta[
            indice
        ].nombre;


    carritoVenta.splice(
        indice,
        1
    );


    renderizarCarrito();


    mostrarAvisoVenta(
        `${nombre} fue retirado de la venta.`,
        "primary"
    );
}


/* ==========================================================
   RESUMEN
   ========================================================== */
function recalcularVenta() {

    let subtotalBruto = 0;
    let descuentoTotal = 0;


    carritoVenta.forEach(
        function (item) {

            const subtotalLinea =
                Number(item.precioUnitario) *
                Number(item.cantidad);


            const descuentoLinea =
                subtotalLinea *
                (
                    Number(item.descuentoPorcentaje) /
                    100
                );


            subtotalBruto +=
                subtotalLinea;


            descuentoTotal +=
                descuentoLinea;
        }
    );


    const total =
        Math.max(
            subtotalBruto -
            descuentoTotal,
            0
        );


    const porcentaje =
        Number(
            CONFIG_VENTA.impuestoPorcentaje || 0
        );


    let impuesto = 0;

    let subtotalSinImpuesto =
        total;


    if (porcentaje > 0) {

        subtotalSinImpuesto =
            total /
            (
                1 +
                (
                    porcentaje /
                    100
                )
            );


        impuesto =
            total -
            subtotalSinImpuesto;
    }


    colocarMontoVenta(
        "subtotalVenta",
        subtotalBruto
    );


    colocarMontoVenta(
        "descuentoVenta",
        descuentoTotal,
        true
    );


    colocarMontoVenta(
        "igvVenta",
        impuesto
    );


    colocarMontoVenta(
        "totalVenta",
        total
    );


    calcularVueltoVenta(
        total
    );


    return {

        subtotal:
            subtotalBruto,

        descuento:
            descuentoTotal,

        impuesto:
            impuesto,

        subtotalSinImpuesto:
            subtotalSinImpuesto,

        total:
            total,

        porcentajeImpuesto:
            porcentaje
    };
}


/* ==========================================================
   CONTADOR
   ========================================================== */

function actualizarContadorProductos() {

    const contador =
        document.getElementById(
            "cantidadProductos"
        );


    if (!contador) {
        return;
    }


    const cantidadLineas =
        carritoVenta.length;


    const unidades =
        carritoVenta.reduce(
            (
                total,
                item
            ) =>
                total +
                item.cantidad,
            0
        );


    contador.textContent =
        cantidadLineas === 1

            ? `${cantidadLineas} producto · ${unidades} und`

            : `${cantidadLineas} productos · ${unidades} und`;
}


/* ==========================================================
   MONTO RECIBIDO / VUELTO
   ========================================================== */

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const montoRecibido =
            document.getElementById(
                "montoRecibido"
            );


        if (montoRecibido) {

            montoRecibido.addEventListener(
                "input",
                function () {

                    recalcularVenta();

                }
            );

        }

    }
);


function calcularVueltoVenta(
    totalVenta
) {

    const input =
        document.getElementById(
            "montoRecibido"
        );


    const vuelto =
        document.getElementById(
            "vueltoVenta"
        );


    if (
        !input ||
        !vuelto
    ) {
        return;
    }


    const recibido =
        Number(
            input.value || 0
        );


    const diferencia =
        recibido -
        totalVenta;


    vuelto.textContent =
        `S/ ${formatearNumeroVenta(
            Math.max(
                diferencia,
                0
            )
        )}`;
}


/* ==========================================================
   HELPERS
   ========================================================== */

function colocarMontoVenta(
    id,
    valor,
    negativo = false
) {

    const elemento =
        document.getElementById(
            id
        );


    if (!elemento) {
        return;
    }


    elemento.textContent =
        negativo
            ? `- S/ ${formatearNumeroVenta(valor)}`
            : `S/ ${formatearNumeroVenta(valor)}`;
}


function formatearNumeroVenta(
    valor
) {

    return Number(
        valor || 0
    ).toLocaleString(
        "es-PE",
        {
            minimumFractionDigits:
                2,

            maximumFractionDigits:
                2
        }
    );
}


function mostrarAvisoVenta(
    mensaje,
    status = "primary"
) {

    if (
        window.UIkit
    ) {

        UIkit.notification({
            message:
                mensaje,

            status:
                status,

            pos:
                "top-right",

            timeout:
                2500
        });


        return;
    }


    console.log(
        mensaje
    );
}/* ==========================================================
   LIMPIAR VENTA
   ========================================================== */

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const boton =
            document.getElementById(
                "limpiarVenta"
            );


        if (!boton) {
            return;
        }


        boton.addEventListener(
            "click",
            function () {

                if (
                    carritoVenta.length === 0 &&
                    !document.getElementById(
                        "idClienteSeleccionado"
                    )?.value
                ) {

                    mostrarAvisoVenta(
                        "La venta ya está vacía.",
                        "primary"
                    );

                    return;
                }


                if (window.UIkit) {

                    UIkit.modal.confirm(
                        "¿Deseas limpiar todos los datos de la venta?"
                    )
                        .then(
                            function () {

                                limpiarVentaCompleta();

                            }
                        )
                        .catch(
                            function () {
                            }
                        );

                    return;
                }


                if (
                    confirm(
                        "¿Deseas limpiar todos los datos de la venta?"
                    )
                ) {

                    limpiarVentaCompleta();

                }

            }
        );

    }
);


function limpiarVentaCompleta() {

    // =========================================================
    // CARRITO
    // =========================================================

    carritoVenta.splice(
        0,
        carritoVenta.length
    );


    // =========================================================
    // CLIENTE
    // =========================================================

    const idCliente =
        document.getElementById(
            "idClienteSeleccionado"
        );

    const buscarCliente =
        document.getElementById(
            "buscarCliente"
        );

    const documento =
        document.getElementById(
            "documentoCliente"
        );

    const telefono =
        document.getElementById(
            "telefonoCliente"
        );

    const clienteSeleccionado =
        document.getElementById(
            "clienteSeleccionado"
        );

    const resultadosClientes =
        document.getElementById(
            "resultadosClientes"
        );


    if (idCliente) {
        idCliente.value = "";
    }

    if (buscarCliente) {
        buscarCliente.value = "";
    }

    if (documento) {
        documento.value = "";
    }

    if (telefono) {
        telefono.value = "";
    }

    if (clienteSeleccionado) {
        clienteSeleccionado.hidden = true;
    }

    if (resultadosClientes) {

        resultadosClientes.innerHTML =
            "";

        resultadosClientes.hidden =
            true;
    }


    // =========================================================
    // PRODUCTOS
    // =========================================================

    const buscarProducto =
        document.getElementById(
            "buscarProducto"
        );

    const productosDisponibles =
        document.getElementById(
            "productosDisponibles"
        );


    if (buscarProducto) {
        buscarProducto.value = "";
    }


    if (productosDisponibles) {

        productosDisponibles.innerHTML = `

            <div class="kxvacioproductos">

                Escribe al menos 2 caracteres
                para buscar productos.

            </div>

        `;
    }


    // =========================================================
    // PAGO
    // =========================================================

    const efectivo =
        document.querySelector(
            'input[name="MetodoPago"][value="Efectivo"]'
        );


    document
        .querySelectorAll(
            'input[name="MetodoPago"]'
        )
        .forEach(
            function (radio) {

                radio.checked =
                    false;

            }
        );


    if (efectivo) {
        efectivo.checked = true;
    }


    const recibido =
        document.getElementById(
            "montoRecibido"
        );


    if (recibido) {
        recibido.value = "0";
    }


    // =========================================================
    // REDIBUJAR
    // =========================================================

    renderizarCarrito();


    mostrarAvisoVenta(
        "La venta fue limpiada.",
        "success"
    );
}/* ==========================================================
   MODAL NUEVO CLIENTE
   ========================================================== */
/* ==========================================================
   NUEVO CLIENTE
   ========================================================== */

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const modal =
            document.getElementById(
                "modalNuevoCliente"
            );

        const abrir =
            document.getElementById(
                "abrirModalNuevoCliente"
            );

        const cerrar =
            document.getElementById(
                "cerrarModalNuevoCliente"
            );

        const cancelar =
            document.getElementById(
                "cancelarNuevoCliente"
            );

        const guardar =
            document.getElementById(
                "guardarNuevoCliente"
            );

        const overlay =
            document.getElementById(
                "overlayNuevoCliente"
            );

        const tipoCliente =
            document.getElementById(
                "nuevoTipoCliente"
            );


        if (!modal) {
            return;
        }


        /* =====================================================
           ABRIR
           ===================================================== */

        abrir?.addEventListener(
            "click",
            function () {

                limpiarFormularioNuevoCliente();

                modal.hidden =
                    false;

                document.body.style.overflow =
                    "hidden";


                actualizarFormularioTipoCliente();


                setTimeout(
                    function () {

                        document
                            .getElementById(
                                "nuevoNumeroDocumento"
                            )
                            ?.focus();

                    },
                    100
                );

            }
        );


        /* =====================================================
           CERRAR X
           ===================================================== */

        cerrar?.addEventListener(
            "click",
            cerrarModalNuevoCliente
        );


        /* =====================================================
           CANCELAR
           ===================================================== */

        cancelar?.addEventListener(
            "click",
            cerrarModalNuevoCliente
        );


        /* =====================================================
           OVERLAY
           ===================================================== */

        overlay?.addEventListener(
            "click",
            cerrarModalNuevoCliente
        );


        /* =====================================================
           ESC
           ===================================================== */

        document.addEventListener(
            "keydown",
            function (event) {

                if (
                    event.key === "Escape"
                    &&
                    modal.hidden === false
                ) {

                    cerrarModalNuevoCliente();

                }

            }
        );


        /* =====================================================
           CAMBIAR TIPO CLIENTE
           ===================================================== */

        tipoCliente?.addEventListener(
            "change",
            actualizarFormularioTipoCliente
        );


        /* =====================================================
           GUARDAR
           ===================================================== */

        guardar?.addEventListener(
            "click",
            async function () {

                await guardarNuevoCliente();

            }
        );

    }
);


/* ==========================================================
   GUARDAR CLIENTE
   ========================================================== */

async function guardarNuevoCliente() {

    const boton =
        document.getElementById(
            "guardarNuevoCliente"
        );


    if (!boton) {
        return;
    }


    ocultarMensajeNuevoCliente();


    /* =====================================================
       OBTENER VALORES
       ===================================================== */

    const tipoCliente =
        obtenerValorNuevoCliente(
            "nuevoTipoCliente"
        )
            .toUpperCase();


    const tipoDocumento =
        obtenerValorNuevoCliente(
            "nuevoTipoDocumento"
        )
            .toUpperCase();


    const numeroDocumento =
        obtenerValorNuevoCliente(
            "nuevoNumeroDocumento"
        );


    const nombres =
        obtenerValorNuevoCliente(
            "nuevoNombres"
        );


    const apellidos =
        obtenerValorNuevoCliente(
            "nuevoApellidos"
        );


    const razonSocial =
        obtenerValorNuevoCliente(
            "nuevoRazonSocial"
        );


    const telefono =
        obtenerValorNuevoCliente(
            "nuevoTelefono"
        );


    const correo =
        obtenerValorNuevoCliente(
            "nuevoCorreo"
        );


    const cargo =
        obtenerValorNuevoCliente(
            "nuevoCargoContacto"
        );


    const tipoDireccion =
        obtenerValorNuevoCliente(
            "nuevoTipoDireccion"
        );


    const direccion =
        obtenerValorNuevoCliente(
            "nuevoDireccion"
        );


    const referencia =
        obtenerValorNuevoCliente(
            "nuevoReferencia"
        );


    const distrito =
        obtenerValorNuevoCliente(
            "nuevoDistrito"
        );


    const provincia =
        obtenerValorNuevoCliente(
            "nuevoProvincia"
        );


    const departamento =
        obtenerValorNuevoCliente(
            "nuevoDepartamento"
        );


    const ubigeo =
        obtenerValorNuevoCliente(
            "nuevoUbigeo"
        );


    /* =====================================================
       VALIDACIONES
       ===================================================== */

    if (!tipoCliente) {

        mostrarMensajeNuevoCliente(
            "Selecciona el tipo de cliente."
        );

        return;
    }


    if (!tipoDocumento) {

        mostrarMensajeNuevoCliente(
            "Selecciona el tipo de documento."
        );

        return;
    }


    if (!numeroDocumento) {

        mostrarMensajeNuevoCliente(
            "Ingresa el número de documento."
        );

        return;
    }


    if (
        tipoDocumento === "DNI"
        &&
        !/^\d{8}$/.test(
            numeroDocumento
        )
    ) {

        mostrarMensajeNuevoCliente(
            "El DNI debe contener 8 dígitos."
        );

        return;
    }


    if (
        tipoDocumento === "RUC"
        &&
        !/^\d{11}$/.test(
            numeroDocumento
        )
    ) {

        mostrarMensajeNuevoCliente(
            "El RUC debe contener 11 dígitos."
        );

        return;
    }


    if (
        tipoCliente === "NATURAL"
        &&
        !nombres
    ) {

        mostrarMensajeNuevoCliente(
            "Ingresa los nombres del cliente."
        );

        return;
    }


    if (
        tipoCliente === "JURIDICO"
        &&
        !razonSocial
    ) {

        mostrarMensajeNuevoCliente(
            "Ingresa la razón social."
        );

        return;
    }


    if (
        correo
        &&
        !validarCorreoCliente(
            correo
        )
    ) {

        mostrarMensajeNuevoCliente(
            "Ingresa un correo válido."
        );

        return;
    }


    /* =====================================================
       CONTACTOS
       ===================================================== */

    const contactos = [];


    if (
        telefono
        ||
        correo
        ||
        cargo
    ) {

        let nombreContacto =
            "";


        if (
            tipoCliente ===
            "JURIDICO"
        ) {

            nombreContacto =
                nombres ||
                razonSocial;

        }
        else {

            nombreContacto =
                `${nombres} ${apellidos}`
                    .trim();

        }


        contactos.push(
            {
                nombres:
                    nombreContacto,

                cargo:
                    cargo,

                telefono:
                    telefono,

                correo:
                    correo,

                esPrincipal:
                    true,

                estado:
                    true
            }
        );

    }


    /* =====================================================
       DIRECCIONES
       ===================================================== */

    const direcciones = [];


    if (direccion) {

        direcciones.push(
            {
                tipoDireccion:
                    tipoDireccion ||
                    "FISCAL",

                direccion:
                    direccion,

                referencia:
                    referencia,

                distrito:
                    distrito,

                provincia:
                    provincia,

                departamento:
                    departamento,

                ubigeo:
                    ubigeo,

                esPrincipal:
                    true,

                estado:
                    true
            }
        );

    }


    /* =====================================================
       REQUEST
       ===================================================== */

    const request =
    {
        idEmpresa:
            Number(
                window
                    .kernixConfiguracionVenta
                    ?.idEmpresa
                || 0
            ),

        tipoCliente:
            tipoCliente,

        tipoDocumento:
            tipoDocumento,

        numeroDocumento:
            numeroDocumento,

        razonSocial:
            razonSocial || null,

        nombres:
            nombres || null,

        apellidos:
            apellidos || null,

        telefono:
            telefono || null,

        correo:
            correo || null,

        contactos:
            contactos,

        direcciones:
            direcciones
    };


    console.log(
        "[KerniX] Cliente request:",
        request
    );


    /* =====================================================
       TOKEN ANTI FORGERY
       ===================================================== */

    const token =
        document.querySelector(
            'input[name="__RequestVerificationToken"]'
        )
            ?.value;


    /* =====================================================
       POST
       ===================================================== */

    try {

        boton.disabled =
            true;


        boton.innerHTML = `
            Guardando...
        `;


        const response =
            await fetch(
                `${window.location.pathname}?handler=CrearCliente`,
                {
                    method:
                        "POST",

                    credentials:
                        "same-origin",

                    headers:
                    {
                        "Content-Type":
                            "application/json",

                        "Accept":
                            "application/json",

                        "RequestVerificationToken":
                            token || ""
                    },

                    body:
                        JSON.stringify(
                            request
                        )
                }
            );


        const texto =
            await response.text();


        let resultado =
            null;


        if (texto) {

            try {

                resultado =
                    JSON.parse(
                        texto
                    );

            }
            catch {

                console.error(
                    "Respuesta no JSON:",
                    texto
                );

            }

        }


        console.log(
            "[KerniX] Crear cliente:",
            resultado
        );


        if (!response.ok) {

            throw new Error(
                resultado?.message
                ||
                `Error HTTP ${response.status}`
            );

        }


        if (
            !resultado
            ||
            resultado.success !== true
        ) {

            throw new Error(
                resultado?.message
                ||
                "No se pudo registrar el cliente."
            );

        }


        if (!resultado.data) {

            throw new Error(
                "El servidor no devolvió el cliente creado."
            );

        }


        /* =================================================
           SELECCIONAR CLIENTE RECIÉN CREADO
           ================================================= */

        seleccionarCliente(
            resultado.data
        );


        /* =================================================
           CERRAR
           ================================================= */

        cerrarModalNuevoCliente();


        /* =================================================
           AVISO
           ================================================= */

        mostrarAvisoVenta(
            "Cliente registrado correctamente.",
            "success"
        );


        console.log(
            "[KerniX] Cliente creado y seleccionado:",
            resultado.data
        );

    }
    catch (error) {

        console.error(
            "[KerniX] Error registrando cliente:",
            error
        );


        mostrarMensajeNuevoCliente(
            error.message
            ||
            "No se pudo registrar el cliente."
        );

    }
    finally {

        boton.disabled =
            false;


        boton.innerHTML = `
            <span uk-icon="icon: check"></span>
            Guardar cliente
        `;


        if (window.UIkit) {

            UIkit.icon(
                boton
            );

        }

    }

}


/* ==========================================================
   CERRAR MODAL
   ========================================================== */

function cerrarModalNuevoCliente() {

    const modal =
        document.getElementById(
            "modalNuevoCliente"
        );


    if (!modal) {
        return;
    }


    modal.hidden =
        true;


    document.body.style.overflow =
        "";


    ocultarMensajeNuevoCliente();
}


/* ==========================================================
   TIPO DE CLIENTE
   ========================================================== */

function actualizarFormularioTipoCliente() {

    const tipo =
        obtenerValorNuevoCliente(
            "nuevoTipoCliente"
        )
            .toUpperCase();


    const razonSocial =
        document.getElementById(
            "nuevoRazonSocial"
        );


    const nombres =
        document.getElementById(
            "nuevoNombres"
        );


    const apellidos =
        document.getElementById(
            "nuevoApellidos"
        );


    const tipoDocumento =
        document.getElementById(
            "nuevoTipoDocumento"
        );


    if (
        tipo ===
        "JURIDICO"
    ) {

        if (razonSocial) {

            razonSocial.disabled =
                false;

        }


        if (tipoDocumento) {

            tipoDocumento.value =
                "RUC";

        }

    }
    else {

        if (razonSocial) {

            razonSocial.value =
                "";

        }


        if (nombres) {

            nombres.disabled =
                false;

        }


        if (apellidos) {

            apellidos.disabled =
                false;

        }


        if (
            tipoDocumento
            &&
            tipoDocumento.value ===
            "RUC"
        ) {

            tipoDocumento.value =
                "DNI";

        }

    }

}


/* ==========================================================
   OBTENER VALOR
   ========================================================== */

function obtenerValorNuevoCliente(
    id
) {

    const elemento =
        document.getElementById(
            id
        );


    if (!elemento) {
        return "";
    }


    return String(
        elemento.value || ""
    )
        .trim();
}


/* ==========================================================
   LIMPIAR FORMULARIO
   ========================================================== */

function limpiarFormularioNuevoCliente() {

    const campos =
        [
            "nuevoNumeroDocumento",
            "nuevoTelefono",
            "nuevoNombres",
            "nuevoApellidos",
            "nuevoRazonSocial",
            "nuevoCorreo",
            "nuevoCargoContacto",
            "nuevoDireccion",
            "nuevoDistrito",
            "nuevoProvincia",
            "nuevoDepartamento",
            "nuevoUbigeo",
            "nuevoReferencia"
        ];


    campos.forEach(
        function (id) {

            const elemento =
                document.getElementById(
                    id
                );


            if (elemento) {

                elemento.value =
                    "";

            }

        }
    );


    const tipoCliente =
        document.getElementById(
            "nuevoTipoCliente"
        );


    const tipoDocumento =
        document.getElementById(
            "nuevoTipoDocumento"
        );


    const tipoDireccion =
        document.getElementById(
            "nuevoTipoDireccion"
        );


    if (tipoCliente) {

        tipoCliente.value =
            "NATURAL";

    }


    if (tipoDocumento) {

        tipoDocumento.value =
            "DNI";

    }


    if (tipoDireccion) {

        tipoDireccion.value =
            "FISCAL";

    }


    ocultarMensajeNuevoCliente();
}


/* ==========================================================
   ERROR MODAL
   ========================================================== */

function mostrarMensajeNuevoCliente(
    mensaje
) {

    const contenedor =
        document.getElementById(
            "mensajeNuevoCliente"
        );


    if (!contenedor) {

        console.error(
            mensaje
        );

        return;
    }


    contenedor.textContent =
        mensaje;


    contenedor.hidden =
        false;
}


function ocultarMensajeNuevoCliente() {

    const contenedor =
        document.getElementById(
            "mensajeNuevoCliente"
        );


    if (!contenedor) {
        return;
    }


    contenedor.textContent =
        "";


    contenedor.hidden =
        true;
}


/* ==========================================================
   VALIDAR CORREO
   ========================================================== */

function validarCorreoCliente(
    correo
) {

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        .test(
            correo
        );
}