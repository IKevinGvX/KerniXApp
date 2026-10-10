/* ==========================================================
   CLIENTES
   ========================================================== */

let numeroBusquedaClientes = 0;
let numeroBusquedaProductos = 0;
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
            "[KerniX] No existe #buscarCliente"
        );

        return;
    }


    if (!resultados) {

        console.warn(
            "[KerniX] No existe #resultadosClientes"
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
            numeroBusquedaClientes++;
            const seleccionado = document.getElementById("idClienteSeleccionado");
            if (seleccionado?.value) {
                seleccionado.value = "";
                const tarjeta = document.getElementById("clienteSeleccionado");
                if (tarjeta) tarjeta.hidden = true;
            }

            console.log(
                "[KerniX] Texto cliente:",
                termino
            );


            if (termino.length < 2) {

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


            if (termino.length >= 2) {

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
                event.key ===
                "Escape"
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

        console.error(
            "[KerniX] No existe #resultadosClientes"
        );

        return;
    }


    const busquedaActual = ++numeroBusquedaClientes;
    resultados.hidden = false;
    resultados.style.display = "block";
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
            "[KerniX] Buscar clientes:",
            url
        );


        const response =
            await fetch(
                url,
                {
                    method:
                        "GET",

                    headers:
                    {
                        "Accept":
                            "application/json"
                    },

                    credentials:
                        "same-origin"
                }
            );


        const texto =
            await response.text();


        console.log(
            "[KerniX] HTTP clientes:",
            response.status
        );


        let resultado =
            null;


        try {

            resultado =
                texto
                    ? JSON.parse(
                        texto
                    )
                    : null;

        }
        catch (error) {

            console.error(
                "[KerniX] JSON clientes inválido:",
                error
            );


            throw new Error(
                "La respuesta del servidor no es JSON válido."
            );
        }


        if (!response.ok) {

            throw new Error(
                resultado?.message ||
                `Error HTTP ${response.status}`
            );
        }


        if (
            !resultado ||
            resultado.success !== true
        ) {

            throw new Error(
                resultado?.message ||
                "No se pudieron cargar los clientes."
            );
        }


        const clientes =
            Array.isArray(
                resultado.data
            )
                ? resultado.data
                : [];


        console.log(
            "[KerniX] Clientes encontrados:",
            clientes.length,
            clientes
        );


        if (busquedaActual !== numeroBusquedaClientes) return;
        mostrarClientes(
            clientes
        );

    }
    catch (error) {
        if (busquedaActual !== numeroBusquedaClientes) return;
        console.error(
            "[KerniX] Error buscarClientes:",
            error
        );


        resultados.hidden =
            false;


        resultados.style.display =
            "block";


        resultados.innerHTML = `
            <div class="kxclienteerror">
                ${escaparHtml(
            error.message ||
            "No fue posible cargar los clientes."
        )}
            </div>
        `;
    }
}


/* ==========================================================
   MOSTRAR CLIENTES
   ========================================================== */

function mostrarClientes(
    clientes
) {

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );


    if (!resultados) {

        console.error(
            "[KerniX] No existe #resultadosClientes."
        );

        return;
    }


    resultados.innerHTML =
        "";


    resultados.hidden =
        false;


    resultados.style.display =
        "block";


    resultados.style.height =
        "auto";


    resultados.style.minHeight =
        "0";


    resultados.style.maxHeight =
        "320px";


    resultados.style.overflowY =
        "auto";


    if (
        !Array.isArray(
            clientes
        )
        ||
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


                <div class="kxresultadoseleccionar">

                    Seleccionar

                    <span uk-icon="icon: chevron-right"></span>

                </div>

            `;


            boton.addEventListener(
                "click",
                function (event) {

                    event.preventDefault();

                    event.stopPropagation();


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


    resultados.style.minHeight =
        "70px";


    console.log(
        "[KerniX] Resultados renderizados:",
        resultados.children.length
    );


    if (window.UIkit) {

        UIkit.icon(
            resultados
        );
    }
}


/* ==========================================================
   CERRAR RESULTADOS CLIENTES
   ========================================================== */

function cerrarResultadosClientes() {
    numeroBusquedaClientes++;

    const resultados =
        document.getElementById(
            "resultadosClientes"
        );


    if (!resultados) {
        return;
    }


    resultados.hidden =
        true;


    resultados.style.display =
        "none";


    resultados.style.minHeight =
        "0";


    resultados.innerHTML =
        "";
}/* ==========================================================
   SELECCIONAR CLIENTE
   ========================================================== */

function seleccionarCliente(
    cliente
) {

    if (!cliente) {
        return;
    }


    const nombre =
        obtenerNombreCliente(
            cliente
        );


    const documentoTexto =
        obtenerDocumentoCliente(
            cliente
        );


    const tipoDocumento =
        String(
            cliente.tipoDocumento || ""
        )
            .trim()
            .toUpperCase();


    const numeroDocumento =
        String(
            cliente.numeroDocumento || ""
        )
            .trim();


    const direccion =
        obtenerDireccionCliente(
            cliente
        );


    const idCliente =
        document.getElementById(
            "idClienteSeleccionado"
        );


    const tipoDocumentoCliente =
        document.getElementById(
            "tipoDocumentoCliente"
        );


    const codigoDocumentoCliente =
        document.getElementById(
            "codigoDocumentoCliente"
        );


    const documentoCliente =
        document.getElementById(
            "documentoCliente"
        );


    const telefonoCliente =
        document.getElementById(
            "telefonoCliente"
        );


    const direccionCliente =
        document.getElementById(
            "direccionCliente"
        );


    const buscarCliente =
        document.getElementById(
            "buscarCliente"
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


    if (tipoDocumentoCliente) {

        tipoDocumentoCliente.value =
            tipoDocumento;
    }


    if (codigoDocumentoCliente) {

        codigoDocumentoCliente.value =
            tipoDocumento;
    }


    if (documentoCliente) {

        documentoCliente.value =
            numeroDocumento;
    }


    if (telefonoCliente) {

        telefonoCliente.value =
            cliente.telefono || "";
    }


    if (direccionCliente) {

        direccionCliente.value =
            direccion;
    }


    if (buscarCliente) {

        buscarCliente.value =
            nombre;
    }


    if (clienteNombre) {

        clienteNombre.textContent =
            nombre;
    }


    if (clienteDocumentoTexto) {

        clienteDocumentoTexto.textContent =
            documentoTexto;
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


    seleccionarComprobantePorCliente(
        tipoDocumento,
        numeroDocumento
    );


    cerrarResultadosClientes();
}


/* ==========================================================
   COMPROBANTE AUTOMÁTICO SEGÚN DOCUMENTO
   ========================================================== */

function seleccionarComprobantePorCliente(
    tipoDocumento,
    numeroDocumento
) {

    const tipo =
        String(
            tipoDocumento || ""
        )
            .trim()
            .toUpperCase();


    const numero =
        String(
            numeroDocumento || ""
        )
            .replace(
                /\D/g,
                ""
            );


    let comprobante =
        "BOLETA";


    if (
        tipo === "RUC"
        &&
        numero.length === 11
    ) {

        comprobante =
            "FACTURA";
    }


    if (
        tipo === "DNI"
        ||
        tipo === "CE"
    ) {

        comprobante =
            "BOLETA";
    }


    const radio =
        document.querySelector(
            `input[name="TipoComprobante"][value="${comprobante}"]`
        );


    if (radio) {

        radio.checked =
            true;
    }


    const campoComprobante =
        document.getElementById(
            "comprobanteCliente"
        );


    if (campoComprobante) {

        campoComprobante.value =
            comprobante;
    }


    console.log(
        "[KerniX] Comprobante automático:",
        comprobante
    );
}


/* ==========================================================
   LIMPIAR CLIENTE SELECCIONADO
   ========================================================== */

function limpiarClienteSeleccionado() {

    const controles =
        [
            "idClienteSeleccionado",
            "buscarCliente",
            "tipoDocumentoCliente",
            "codigoDocumentoCliente",
            "documentoCliente",
            "telefonoCliente",
            "direccionCliente",
            "comprobanteCliente"
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


    const radioBoleta =
        document.querySelector(
            'input[name="TipoComprobante"][value="BOLETA"]'
        );


    if (radioBoleta) {

        radioBoleta.checked =
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
   HELPERS CLIENTE
   ========================================================== */

function obtenerNombreCliente(
    cliente
) {

    if (!cliente) {
        return "Cliente";
    }


    if (cliente.nombreCompleto) {

        return String(
            cliente.nombreCompleto
        ).trim();
    }


    if (cliente.razonSocial) {

        return String(
            cliente.razonSocial
        ).trim();
    }


    const nombres =
        String(
            cliente.nombres || ""
        ).trim();


    const apellidos =
        String(
            cliente.apellidos || ""
        ).trim();


    const completo =
        `${nombres} ${apellidos}`
            .trim();


    return completo ||
        "Cliente";
}


function obtenerDocumentoCliente(
    cliente
) {

    if (!cliente) {
        return "";
    }


    const tipo =
        String(
            cliente.tipoDocumento || ""
        )
            .trim()
            .toUpperCase();


    const numero =
        String(
            cliente.numeroDocumento || ""
        )
            .trim();


    return `${tipo} ${numero}`
        .trim();
}


function obtenerDireccionCliente(
    cliente
) {

    if (!cliente) {
        return "";
    }


    if (
        cliente.direccion
    ) {

        return String(
            cliente.direccion
        ).trim();
    }


    if (
        cliente.direccionPrincipal
    ) {

        return String(
            cliente.direccionPrincipal
        ).trim();
    }


    if (
        Array.isArray(
            cliente.direcciones
        )
        &&
        cliente.direcciones.length > 0
    ) {

        const principal =
            cliente.direcciones.find(
                x =>
                    x.esPrincipal === true
            )
            ||
            cliente.direcciones[0];


        if (!principal) {
            return "";
        }


        return [
            principal.direccion,
            principal.distrito,
            principal.provincia,
            principal.departamento
        ]
            .filter(
                x =>
                    x &&
                    String(x).trim()
            )
            .join(
                ", "
            );
    }


    return "";
}


function obtenerInicial(
    valor
) {

    const texto =
        String(
            valor || ""
        )
            .trim();


    if (!texto) {
        return "C";
    }


    return texto
        .charAt(0)
        .toUpperCase();
}


function escaparHtml(
    valor
) {

    const div =
        document.createElement(
            "div"
        );


    div.textContent =
        String(
            valor ?? ""
        );


    return div.innerHTML;
}/* ==========================================================
   PRODUCTOS
   ========================================================== */

let temporizadorProductos =
    null;


/* ==========================================================
   INICIAR BUSCADOR PRODUCTOS
   ========================================================== */

function iniciarBuscadorProductos() {

    const input =
        document.getElementById(
            "buscarProducto"
        );


    if (!input) {

        console.warn(
            "[KerniX] No existe #buscarProducto"
        );

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


            console.log(
                "[KerniX] Texto producto:",
                termino
            );


            if (termino.length < 2) {
                numeroBusquedaProductos++;
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


/* ==========================================================
   BUSCAR PRODUCTOS
   ========================================================== */

async function buscarProductos(
    termino
) {
    const busquedaActual = ++numeroBusquedaProductos;

    mostrarMensajeProductos(
        "Buscando productos..."
    );


    try {

        const url =
            `${window.location.pathname}?handler=BuscarProductos&termino=${encodeURIComponent(
                termino
            )}`;


        console.log(
            "[KerniX] Buscar productos:",
            url
        );


        const response =
            await fetch(
                url,
                {
                    method:
                        "GET",

                    headers:
                    {
                        "Accept":
                            "application/json"
                    },

                    credentials:
                        "same-origin"
                }
            );


        const texto =
            await response.text();


        console.log(
            "[KerniX] HTTP productos:",
            response.status
        );


        let resultado =
            null;


        try {

            resultado =
                texto
                    ? JSON.parse(
                        texto
                    )
                    : null;

        }
        catch (error) {

            console.error(
                "[KerniX] JSON productos inválido:",
                error
            );


            throw new Error(
                "La respuesta de productos no es JSON válido."
            );
        }


        if (!response.ok) {

            throw new Error(
                resultado?.message ||
                `Error HTTP ${response.status}`
            );
        }


        if (
            !resultado ||
            resultado.success !== true
        ) {

            throw new Error(
                resultado?.message ||
                "No se pudieron cargar los productos."
            );
        }


        const productos =
            Array.isArray(
                resultado.data
            )
                ? resultado.data
                : [];


        console.log(
            "[KerniX] Productos encontrados:",
            productos.length,
            productos
        );


        if (busquedaActual !== numeroBusquedaProductos) return;
        mostrarProductos(
            productos
        );

    }
    catch (error) {
        if (busquedaActual !== numeroBusquedaProductos) return;
        console.error(
            "[KerniX] Error buscarProductos:",
            error
        );


        mostrarMensajeProductos(
            error.message ||
            "No fue posible cargar productos."
        );
    }
}


/* ==========================================================
   MOSTRAR PRODUCTOS
   ========================================================== */

function mostrarProductos(
    productos
) {

    const contenedor =
        document.getElementById(
            "productosDisponibles"
        );


    if (!contenedor) {

        console.error(
            "[KerniX] No existe #productosDisponibles"
        );

        return;
    }


    contenedor.innerHTML =
        "";


    if (
        !Array.isArray(
            productos
        )
        ||
        productos.length === 0
    ) {

        mostrarMensajeProductos(
            "No se encontraron productos."
        );

        return;
    }


    productos.forEach(
        function (producto) {

            const idProducto =
                Number(
                    producto.idProducto || 0
                );


            const nombre =
                String(
                    producto.nombre || "Producto"
                );


            const codigo =
                String(
                    producto.codigo || ""
                );


            const categoria =
                String(
                    producto.categoria || ""
                );


            const precio =
                Number(
                    producto.precioVenta || 0
                );


            const stockDisponible =
                Number(
                    producto.stockDisponible || 0
                );


            const controlaStock =
                producto.controlaStock === true;


            const sinStock =
                controlaStock
                &&
                stockDisponible <= 0
                &&
                !CONFIG_VENTA
                    .permitirVentaSinStock;


            const card =
                document.createElement(
                    "div"
                );


            card.className =
                "kxproductocard";


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
                nombre
            )}
                    </strong>

                    <span>
                        ${escaparHtml(
                codigo
            )}
                    </span>

                    <small>
                        ${escaparHtml(
                categoria
            )}

                        · Stock:

                        ${stockDisponible}

                        ${escaparHtml(
                producto.abreviaturaUnidad ||
                "und"
            )}
                    </small>

                </div>

                <div class="kxproductoacciones">

                    <div class="kxproductoprecio">
                        S/
                        ${precio.toFixed(2)}
                    </div>

                    ${sinStock
                    ?
                    `
                            <button type="button"
                                    class="kxproducto-agregar deshabilitado"
                                    disabled>
                                Sin stock
                            </button>
                            `
                    :
                    `
                            <button type="button"
                                    class="kxproducto-agregar">

                                <span uk-icon="icon: plus"></span>

                                Agregar
                            </button>
                            `
                }

                </div>
            `;


            const botonAgregar =
                card.querySelector(
                    ".kxproducto-agregar"
                );


            if (
                botonAgregar
                &&
                !sinStock
            ) {

                botonAgregar.addEventListener(
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


/* ==========================================================
   MENSAJE PRODUCTOS
   ========================================================== */

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
}


/* ==========================================================
   CARRITO
   ========================================================== */

const carritoVenta =
    [];


/* ==========================================================
   CONFIGURACIÓN VENTA
   ========================================================== */

const CONFIG_VENTA =
{
    porcentajeImpuesto:
        Number(
            window
                .kernixConfiguracionVenta
                ?.porcentajeImpuesto
            ?? 0
        ),

    descuentoMaximoPorcentaje:
        Number(
            window
                .kernixConfiguracionVenta
                ?.descuentoMaximo
            ?? 0
        ),

    permitirVentaSinStock:
        window
            .kernixConfiguracionVenta
            ?.permitirVentaSinStock
        === true,

    validarStockAntesVenta:
        window
            .kernixConfiguracionVenta
            ?.validarStockAntesVenta
        !== false,

    permitirCambioPrecio:
        window
            .kernixConfiguracionVenta
            ?.permitirCambioPrecio
        === true,

    permitirVentaCredito:
        window
            .kernixConfiguracionVenta
            ?.permitirVentaCredito
        === true,

    idEmpresa:
        Number(
            window
                .kernixConfiguracionVenta
                ?.idEmpresa
            ?? 0
        ),

    idSucursal:
        Number(
            window
                .kernixConfiguracionVenta
                ?.idSucursal
            ?? 0
        ),

    idAlmacen:
        Number(
            window
                .kernixConfiguracionVenta
                ?.idAlmacen
            ?? 0
        )
};


console.log(
    "[KerniX] Configuración venta:",
    CONFIG_VENTA
);


/* ==========================================================
   AGREGAR PRODUCTO AL CARRITO
   ========================================================== */

function agregarProductoCarrito(
    producto
) {

    if (!producto) {
        return;
    }


    const idProducto =
        Number(
            producto.idProducto || 0
        );


    if (idProducto <= 0) {

        mostrarAvisoVenta(
            "El producto seleccionado no es válido.",
            "danger"
        );

        return;
    }


    const stockDisponible =
        Number(
            producto.stockDisponible || 0
        );


    const controlaStock =
        producto.controlaStock === true;


    if (
        controlaStock
        &&
        stockDisponible <= 0
        &&
        !CONFIG_VENTA
            .permitirVentaSinStock
    ) {

        mostrarAvisoVenta(
            "El producto no tiene stock disponible.",
            "warning"
        );

        return;
    }


    const precioProducto = Number(producto.precioVenta);
    if (!Number.isFinite(precioProducto) || precioProducto <= 0) {
        mostrarAvisoVenta("El producto no tiene un precio de venta válido.", "warning");
        return;
    }

    const existente =
        carritoVenta.find(
            item =>
                item.idProducto ===
                idProducto
        );


    if (existente) {

        const nuevaCantidad =
            Number(
                existente.cantidad
            )
            + 1;


        if (
            controlaStock
            &&
            CONFIG_VENTA
                .validarStockAntesVenta
            &&
            !CONFIG_VENTA
                .permitirVentaSinStock
            &&
            nuevaCantidad >
            stockDisponible
        ) {

            mostrarAvisoVenta(
                `Solo hay ${stockDisponible} unidades disponibles.`,
                "warning"
            );

            return;
        }


        existente.cantidad =
            nuevaCantidad;

    }
    else {

        carritoVenta.push(
            {
                idProducto:
                    idProducto,

                codigo:
                    producto.codigo || "",

                nombre:
                    producto.nombre ||
                    "Producto",

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
                    producto.abreviaturaUnidad ||
                    "und"
            }
        );
    }


    renderizarCarrito();


    mostrarAvisoVenta(
        `${producto.nombre || "Producto"} agregado a la venta.`,
        "success"
    );
}
/* ==========================================================
   RENDERIZAR CARRITO
   ========================================================== */

function renderizarCarrito() {

    const tbody =
        document.getElementById(
            "detalleVenta"
        );


    if (!tbody) {

        console.warn(
            "[KerniX] No existe #detalleVenta"
        );

        return;
    }


    tbody.innerHTML =
        "";


    if (
        carritoVenta.length === 0
    ) {

        tbody.innerHTML = `
            <tr id="filaVacia">

                <td colspan="7"
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

            const precioUnitario =
                Number(
                    item.precioUnitario || 0
                );


            const cantidad =
                Number(
                    item.cantidad || 0
                );


            const descuentoPorcentaje =
                Number(
                    item.descuentoPorcentaje || 0
                );


            const subtotalBruto =
                precioUnitario
                *
                cantidad;


            const descuentoMonto =
                subtotalBruto
                *
                (
                    descuentoPorcentaje
                    /
                    100
                );


            const subtotalNeto =
                Math.max(
                    0,
                    subtotalBruto
                    -
                    descuentoMonto
                );


            const porcentajeIGV =
                Number(
                    CONFIG_VENTA
                        .porcentajeImpuesto
                    || 0
                );


            // Misma lógica funcional que SQL:
            // (cantidad * precio_unitario - descuento) * IGV
            const igvLinea =
                porcentajeIGV > 0
                    ?
                    subtotalNeto
                    *
                    (
                        porcentajeIGV
                        /
                        100
                    )
                    :
                    0;


            const totalLinea =
                subtotalNeto
                +
                igvLinea;


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

                    <div class="kxprecioproducto">

                        <strong>
                            S/
                            ${formatearNumeroVenta(
                precioUnitario
            )}
                        </strong>

                        <small>
                            Sin IGV
                        </small>

                    </div>

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
                               value="${cantidad}" />


                        <button type="button"
                                class="kxcantidadboton"
                                data-accion="sumar"
                                data-id="${item.idProducto}">
                            +
                        </button>

                    </div>


                    <small class="kxstockcarrito">

                        Disponible:
                        ${Number(
                item.stockDisponible || 0
            )}

                        ${escaparHtml(
                item.abreviaturaUnidad ||
                "und"
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
                               value="${descuentoPorcentaje}" />

                        <span>
                            %
                        </span>

                    </div>


                    <small class="kxdescuentolimite">

                        - S/
                        ${formatearNumeroVenta(
                descuentoMonto
            )}

                    </small>

                </td>


                <td>

                    <div class="kxigvproducto">

                        <strong>
                            S/
                            ${formatearNumeroVenta(
                igvLinea
            )}
                        </strong>

                        <small>
                            ${formatearNumeroVenta(
                porcentajeIGV
            )}%
                        </small>

                    </div>

                </td>


                <td>

                    <div class="kxtotalproducto">

                        <strong class="kxtotallinea">

                            S/
                            ${formatearNumeroVenta(
                totalLinea
            )}

                        </strong>

                        <small>
                            Incluye IGV
                        </small>

                    </div>

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
                            String(
                                boton.dataset.accion || ""
                            );


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
   CAMBIAR CANTIDAD
   ========================================================== */

function cambiarCantidadProducto(
    idProducto,
    accion
) {

    const item =
        carritoVenta.find(
            x =>
                x.idProducto ===
                idProducto
        );


    if (!item) {
        return;
    }


    let nuevaCantidad =
        Number(
            item.cantidad || 1
        );


    if (accion === "sumar") {

        nuevaCantidad++;
    }


    if (accion === "restar") {

        nuevaCantidad--;
    }


    establecerCantidadProducto(
        idProducto,
        nuevaCantidad
    );
}


/* ==========================================================
   ESTABLECER CANTIDAD
   ========================================================== */

function establecerCantidadProducto(
    idProducto,
    cantidad
) {

    const item =
        carritoVenta.find(
            x =>
                x.idProducto ===
                idProducto
        );


    if (!item) {
        return;
    }


    let nuevaCantidad =
        Math.floor(
            Number(
                cantidad
            )
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
        item.controlaStock
        &&
        CONFIG_VENTA
            .validarStockAntesVenta
        &&
        !CONFIG_VENTA
            .permitirVentaSinStock
        &&
        nuevaCantidad >
        Number(
            item.stockDisponible || 0
        )
    ) {

        mostrarAvisoVenta(
            `Stock disponible: ${item.stockDisponible}.`,
            "warning"
        );


        nuevaCantidad =
            Math.max(
                1,
                Number(
                    item.stockDisponible || 1
                )
            );
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
                x.idProducto ===
                idProducto
        );


    if (!item) {
        return;
    }


    let nuevoDescuento =
        Number(
            descuento
        );


    if (
        !Number.isFinite(
            nuevoDescuento
        )
    ) {

        nuevoDescuento =
            0;
    }


    if (
        nuevoDescuento < 0
    ) {

        nuevoDescuento =
            0;
    }


    if (
        nuevoDescuento >
        CONFIG_VENTA
            .descuentoMaximoPorcentaje
    ) {

        mostrarAvisoVenta(
            `El descuento máximo permitido es ${CONFIG_VENTA.descuentoMaximoPorcentaje}%.`,
            "warning"
        );


        nuevoDescuento =
            CONFIG_VENTA
                .descuentoMaximoPorcentaje;
    }


    item.descuentoPorcentaje =
        nuevoDescuento;


    renderizarCarrito();
}


/* ==========================================================
   QUITAR PRODUCTO
   ========================================================== */

function quitarProductoCarrito(
    idProducto
) {

    const indice =
        carritoVenta.findIndex(
            x =>
                x.idProducto ===
                idProducto
        );


    if (
        indice < 0
    ) {
        return;
    }


    const producto =
        carritoVenta[indice];


    carritoVenta.splice(
        indice,
        1
    );


    renderizarCarrito();


    mostrarAvisoVenta(
        `${producto.nombre} fue retirado de la venta.`,
        "primary"
    );
}


/* ==========================================================
   CONTADOR DE PRODUCTOS
   ========================================================== */

function actualizarContadorProductos() {

    const contador =
        document.getElementById(
            "cantidadProductos"
        );


    if (!contador) {
        return;
    }


    const cantidad =
        carritoVenta.reduce(
            (
                acumulado,
                item
            ) =>
                acumulado
                +
                Number(
                    item.cantidad || 0
                ),
            0
        );


    contador.textContent =
        cantidad === 1
            ? "1 producto"
            : `${cantidad} productos`;
}


/* ==========================================================
   CALCULAR VENTA
   ========================================================== */

function calcularVenta() {

    let subtotalBruto =
        0;

    let descuentoTotal =
        0;

    let impuestoTotal =
        0;

    let totalGeneral =
        0;


    const porcentajeImpuesto =
        Number(
            CONFIG_VENTA
                .porcentajeImpuesto
            || 0
        );


    carritoVenta.forEach(
        function (item) {

            const cantidad =
                Number(
                    item.cantidad || 0
                );


            const precio =
                Number(
                    item.precioUnitario || 0
                );


            const porcentajeDescuento =
                Number(
                    item.descuentoPorcentaje || 0
                );


            const subtotalLinea =
                precio
                *
                cantidad;


            const descuentoLinea =
                subtotalLinea
                *
                (
                    porcentajeDescuento
                    /
                    100
                );


            const baseLinea =
                Math.max(
                    0,
                    subtotalLinea
                    -
                    descuentoLinea
                );


            const impuestoLinea =
                porcentajeImpuesto > 0
                    ?
                    baseLinea
                    *
                    (
                        porcentajeImpuesto
                        /
                        100
                    )
                    :
                    0;


            const totalLinea =
                baseLinea
                +
                impuestoLinea;


            subtotalBruto +=
                subtotalLinea;


            descuentoTotal +=
                descuentoLinea;


            impuestoTotal +=
                impuestoLinea;


            totalGeneral +=
                totalLinea;
        }
    );


    return {

        subtotalBruto:
            subtotalBruto,

        descuento:
            descuentoTotal,

        subtotal:
            Math.max(
                0,
                subtotalBruto
                -
                descuentoTotal
            ),

        impuesto:
            impuestoTotal,

        total:
            totalGeneral
    };
}


/* ==========================================================
   RECALCULAR RESUMEN
   ========================================================== */

function recalcularVenta() {

    const calculo =
        calcularVenta();


    const subtotal =
        document.getElementById(
            "subtotalVenta"
        );


    const descuento =
        document.getElementById(
            "descuentoVenta"
        );


    const igv =
        document.getElementById(
            "igvVenta"
        );


    const total =
        document.getElementById(
            "totalVenta"
        );


    if (subtotal) {

        subtotal.textContent =
            `S/ ${formatearNumeroVenta(
                calculo.subtotal
            )}`;
    }


    if (descuento) {

        descuento.textContent =
            `- S/ ${formatearNumeroVenta(
                calculo.descuento
            )}`;
    }


    if (igv) {

        igv.textContent =
            `S/ ${formatearNumeroVenta(
                calculo.impuesto
            )}`;
    }


    if (total) {

        total.textContent =
            `S/ ${formatearNumeroVenta(
                calculo.total
            )}`;
    }


    recalcularVuelto(
        calculo.total
    );
}


/* ==========================================================
   VUELTO
   ========================================================== */

function recalcularVuelto(
    totalVenta
) {

    const montoRecibido =
        document.getElementById(
            "montoRecibido"
        );


    const vueltoVenta =
        document.getElementById(
            "vueltoVenta"
        );


    if (!vueltoVenta) {
        return;
    }


    const metodoPago =
        document.querySelector(
            'input[name="MetodoPago"]:checked'
        )
            ?.value
            ?.trim()
        || "Efectivo";


    if (
        metodoPago.toUpperCase()
        !== "EFECTIVO"
    ) {

        vueltoVenta.textContent =
            "S/ 0.00";

        return;
    }


    const recibido =
        Number(
            montoRecibido?.value || 0
        );


    const total =
        Number(
            totalVenta || 0
        );


    let vuelto =
        recibido
        -
        total;


    if (
        !Number.isFinite(
            vuelto
        )
        ||
        vuelto < 0
    ) {

        vuelto =
            0;
    }


    vueltoVenta.textContent =
        `S/ ${formatearNumeroVenta(
            vuelto
        )}`;
}


/* ==========================================================
   FORMATO DE NÚMERO
   ========================================================== */

function formatearNumeroVenta(
    valor
) {

    const numero =
        Number(
            valor || 0
        );


    if (
        !Number.isFinite(
            numero
        )
    ) {

        return "0.00";
    }


    return numero.toFixed(
        2
    );
}


/* ==========================================================
   EVENTOS DE PAGO
   ========================================================== */

function iniciarEventosPago() {

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


    document
        .querySelectorAll(
            'input[name="MetodoPago"]'
        )
        .forEach(
            function (radio) {

                radio.addEventListener(
                    "change",
                    function () {

                        const metodo =
                            String(
                                radio.value || ""
                            )
                                .trim()
                                .toUpperCase();


                        if (
                            montoRecibido
                            &&
                            metodo !== "EFECTIVO"
                        ) {

                            const calculo =
                                calcularVenta();


                            montoRecibido.value =
                                calculo.total.toFixed(
                                    2
                                );
                        }


                        recalcularVenta();
                    }
                );
            }
        );
}
function iniciarModalNuevoCliente() {

    const abrir =
        document.getElementById(
            "abrirModalNuevoCliente"
        );

    const cancelar =
        document.getElementById(
            "cancelarNuevoCliente"
        );

    const cerrar =
        document.getElementById(
            "cerrarModalNuevoCliente"
        );

    const overlay =
        document.getElementById(
            "overlayNuevoCliente"
        );

    const guardar =
        document.getElementById(
            "guardarNuevoCliente"
        );


    if (abrir) {

        abrir.onclick =
            function (event) {

                event.preventDefault();
                event.stopPropagation();

                abrirModalNuevoCliente();
            };
    }


    if (cerrar) {

        cerrar.onclick =
            function (event) {

                event.preventDefault();
                event.stopPropagation();

                cerrarModalNuevoCliente();
            };
    }


    if (cancelar) {

        cancelar.onclick =
            function (event) {

                event.preventDefault();
                event.stopPropagation();

                console.log(
                    "[KerniX] CLICK CANCELAR"
                );

                cerrarModalNuevoCliente();
            };
    }


    if (overlay) {

        overlay.onclick =
            function (event) {

                event.preventDefault();

                cerrarModalNuevoCliente();
            };
    }


    if (guardar) {

        guardar.onclick =
            function (event) {

                event.preventDefault();
                event.stopPropagation();

                guardarNuevoCliente();
            };
    }


    const tipoCliente =
        document.getElementById(
            "nuevoTipoCliente"
        );

    if (tipoCliente) {

        tipoCliente.onchange =
            actualizarFormularioTipoCliente;
    }


    const tipoDocumento =
        document.getElementById(
            "nuevoTipoDocumento"
        );

    if (tipoDocumento) {

        tipoDocumento.onchange =
            actualizarDocumentoNuevoCliente;
    }
}
/* ==========================================================
   ABRIR MODAL
   ========================================================== */

let focoAnteriorModal = null;
let scrollAnteriorModal = null;
function abrirModalNuevoCliente() {
    const modal = document.getElementById("modalNuevoCliente");
    if (!modal || !modal.hidden) return;
    focoAnteriorModal = document.activeElement;
    scrollAnteriorModal = document.body.style.overflow;
    limpiarFormularioNuevoCliente();
    actualizarFormularioTipoCliente();
    actualizarDocumentoNuevoCliente();
    modal.hidden = false;
    document.body.style.overflow = "hidden";
    document.getElementById("nuevoNumeroDocumento")?.focus();
}
function cerrarModalNuevoCliente() {
    const modal = document.getElementById("modalNuevoCliente");
    if (!modal || modal.hidden) return;
    modal.hidden = true;
    document.body.style.overflow = scrollAnteriorModal ?? "";
    ocultarMensajeNuevoCliente();
    if (focoAnteriorModal?.isConnected) focoAnteriorModal.focus();
}

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

            razonSocial.placeholder =
                "Ej. Comercial Mejía S.A.C.";
        }


        if (nombres) {

            nombres.disabled =
                true;

            nombres.value =
                "";
        }


        if (apellidos) {

            apellidos.disabled =
                true;

            apellidos.value =
                "";
        }


        if (tipoDocumento) {

            tipoDocumento.value =
                "RUC";
        }

    }
    else {

        if (razonSocial) {

            razonSocial.disabled =
                true;

            razonSocial.value =
                "";

            razonSocial.placeholder =
                "Solo para persona jurídica";
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


    actualizarDocumentoNuevoCliente();
}


/* ==========================================================
   CONFIGURAR DOCUMENTO
   ========================================================== */

function actualizarDocumentoNuevoCliente() {

    const tipoDocumento =
        obtenerValorNuevoCliente(
            "nuevoTipoDocumento"
        )
            .toUpperCase();


    const input =
        document.getElementById(
            "nuevoNumeroDocumento"
        );


    if (!input) {
        return;
    }


    input.inputMode =
        "numeric";


    if (
        tipoDocumento === "DNI"
    ) {

        input.maxLength =
            8;

        input.placeholder =
            "8 dígitos";

        return;
    }


    if (
        tipoDocumento === "RUC"
    ) {

        input.maxLength =
            11;

        input.placeholder =
            "11 dígitos";

        return;
    }


    if (
        tipoDocumento === "CE"
    ) {

        input.maxLength =
            20;

        input.inputMode =
            "text";

        input.placeholder =
            "Carné de extranjería";
    }
}


/* ==========================================================
   GUARDAR NUEVO CLIENTE
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


    const telefono =
        obtenerValorNuevoCliente(
            "nuevoTelefono"
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


    const referencia =
        obtenerValorNuevoCliente(
            "nuevoReferencia"
        );

    /* ======================================================
       VALIDACIONES
       ====================================================== */

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
            "El DNI debe contener exactamente 8 dígitos."
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
            "El RUC debe contener exactamente 11 dígitos."
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


    /* ======================================================
       CONTACTOS
       ====================================================== */

    const contactos =
        [];


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
                    cargo || null,

                telefono:
                    telefono || null,

                correo:
                    correo || null,

                esPrincipal:
                    true,

                estado:
                    true
            }
        );
    }


    /* ======================================================
       DIRECCIONES
       ====================================================== */

    const direcciones =
        [];


    if (direccion) {

        direcciones.push(
            {
                tipoDireccion:
                    tipoDireccion ||
                    "FISCAL",

                direccion:
                    direccion,

                referencia:
                    referencia || null,

                distrito:
                    distrito || null,

                provincia:
                    provincia || null,

                departamento:
                    departamento || null,

                ubigeo:
                    ubigeo || null,

                esPrincipal:
                    true,

                estado:
                    true
            }
        );
    }


    /* ======================================================
       REQUEST CLIENTE
       ====================================================== */

    const request =
    {
        idEmpresa:
            Number(
                window
                    .kernixConfiguracionVenta
                    ?.idEmpresa || 1
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
        "[KerniX] Crear cliente request:",
        request
    );


    /* ======================================================
       TOKEN
       ====================================================== */

    const token =
        document.querySelector(
            '#formTokenNuevaVenta input[name="__RequestVerificationToken"]'
        )
            ?.value
        || "";


    try {

        boton.disabled =
            true;


        boton.innerHTML = `
        <span uk-spinner="ratio: .7"></span>
        Guardando...
    `;


        const response =
            await fetch(
                `${window.location.pathname}?handler=CrearCliente`,
                {
                    method:
                        "POST",

                    headers:
                    {
                        "Content-Type":
                            "application/json",

                        "Accept":
                            "application/json",

                        "RequestVerificationToken":
                            token
                    },

                    credentials:
                        "same-origin",

                    body:
                        JSON.stringify(
                            request
                        )
                }
            );


        const texto =
            await response.text();


        console.log(
            "[KerniX] HTTP CrearCliente:",
            response.status
        );


        console.log(
            "[KerniX] RAW CrearCliente:",
            texto
        );


        let resultado =
            null;


        try {

            resultado =
                texto
                    ? JSON.parse(
                        texto
                    )
                    : null;

        }
        catch (error) {

            console.error(
                "[KerniX] JSON CrearCliente inválido:",
                error
            );


            throw new Error(
                "El servidor devolvió una respuesta inválida."
            );
        }


        if (!response.ok) {

            throw new Error(
                resultado?.message ||
                `Error HTTP ${response.status}`
            );
        }


        if (
            !resultado ||
            resultado.success !== true
        ) {

            throw new Error(
                resultado?.message ||
                "No se pudo registrar el cliente."
            );
        }


        if (!resultado.data) {

            throw new Error(
                "El servidor no devolvió el cliente creado."
            );
        }


        seleccionarCliente(
            resultado.data
        );


        cerrarModalNuevoCliente();


        mostrarAvisoVenta(
            "Cliente registrado correctamente.",
            "success"
        );


        console.log(
            "[KerniX] Cliente creado correctamente:",
            resultado.data
        );

    }
    catch (error) {

        console.error(
            "[KerniX] Error guardarNuevoCliente:",
            error
        );


        mostrarMensajeNuevoCliente(
            error.message ||
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
   HELPERS MODAL NUEVO CLIENTE
   ========================================================== */


/* ==========================================================
   HELPERS MODAL NUEVO CLIENTE
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


function validarCorreoCliente(
    correo
) {

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        .test(
            correo
        );
}/* ==========================================================
   TIPO DE COMPROBANTE
   ========================================================== */

function obtenerTipoComprobanteSeleccionado() {

    const seleccionado =
        document.querySelector(
            'input[name="TipoComprobante"]:checked'
        );


    if (!seleccionado) {

        return "BOLETA";
    }


    return String(
        seleccionado.value || "BOLETA"
    )
        .trim()
        .toUpperCase();
}


/* ==========================================================
   VALIDAR TIPO DE COMPROBANTE
   ========================================================== */

function validarTipoComprobanteSeleccionado() {

    const comprobante =
        obtenerTipoComprobanteSeleccionado();


    const tipoDocumento =
        String(
            document.getElementById(
                "tipoDocumentoCliente"
            )?.value || ""
        )
            .trim()
            .toUpperCase();


    const numeroDocumento =
        String(
            document.getElementById(
                "documentoCliente"
            )?.value || ""
        )
            .trim();


    const numero =
        numeroDocumento.replace(
            /\D/g,
            ""
        );


    if (
        comprobante ===
        "SIN_COMPROBANTE"
    ) {

        return true;
    }


    if (
        comprobante ===
        "NOTA_VENTA"
    ) {

        return true;
    }


    if (
        comprobante ===
        "BOLETA"
    ) {

        if (
            tipoDocumento === "DNI"
            &&
            numero.length !== 8
        ) {

            mostrarAvisoVenta(
                "El DNI debe contener exactamente 8 dígitos.",
                "warning"
            );

            return false;
        }


        return true;
    }


    if (
        comprobante ===
        "FACTURA"
    ) {

        if (
            tipoDocumento !== "RUC"
        ) {

            mostrarAvisoVenta(
                "Para emitir una factura el cliente debe tener RUC.",
                "warning"
            );

            return false;
        }


        if (
            numero.length !== 11
        ) {

            mostrarAvisoVenta(
                "El RUC debe contener exactamente 11 dígitos.",
                "warning"
            );

            return false;
        }


        return true;
    }


    mostrarAvisoVenta(
        "El tipo de comprobante seleccionado no es válido.",
        "warning"
    );


    return false;
}


/* ==========================================================
   ÚLTIMA VENTA / COMPROBANTE
   ========================================================== */

let ultimaVentaRegistrada =
    null;

let ultimoComprobanteEmitido =
    null;


/* ==========================================================
   CONFIRMAR VENTA
   ========================================================== */

async function confirmarVentaServidor() {

    const boton =
        document.getElementById(
            "confirmarVenta"
        );

    if (boton?.disabled) return;

    if (!boton) {

        console.error(
            "[KerniX] No existe #confirmarVenta"
        );

        return;
    }


    if (
        carritoVenta.length === 0
    ) {

        mostrarAvisoVenta(
            "Agrega al menos un producto a la venta.",
            "warning"
        );

        return;
    }


    const idCliente =
        Number(
            document.getElementById(
                "idClienteSeleccionado"
            )?.value || 0
        );


    if (
        idCliente <= 0
    ) {

        mostrarAvisoVenta(
            "Selecciona un cliente.",
            "warning"
        );

        return;
    }


    if (
        !validarTipoComprobanteSeleccionado()
    ) {

        return;
    }


    const tipoComprobante =
        obtenerTipoComprobanteSeleccionado();


    const metodoPago =
        String(
            document.querySelector(
                'input[name="MetodoPago"]:checked'
            )?.value || ""
        )
            .trim();


    if (!metodoPago) {

        mostrarAvisoVenta(
            "Selecciona un método de pago.",
            "warning"
        );

        return;
    }


    const calculo =
        calcularVenta();


    const montoRecibido =
        Number(
            document.getElementById(
                "montoRecibido"
            )?.value || 0
        );


    if (
        metodoPago.toUpperCase() ===
        "EFECTIVO"
        &&
        montoRecibido <
        calculo.total
    ) {

        mostrarAvisoVenta(
            "El monto recibido es menor al total de la venta.",
            "warning"
        );

        return;
    }
    // URL sólo identifica el pedido; el servidor/SQL deben validarlo.
    const parametroPedido = new URLSearchParams(window.location.search).get("idPedido");
    const idPedido = parametroPedido === null ? null : Number(parametroPedido);
    if (parametroPedido !== null && (!Number.isSafeInteger(idPedido) || idPedido <= 0)) {
        mostrarAvisoVenta("El identificador del pedido no es válido.", "warning");
        return;
    }

    const request =
    {
        idPedido: idPedido,
        idCliente:
            idCliente,

        metodoPago:
            metodoPago,

        tipoComprobante:
            tipoComprobante,

        montoRecibido:
            metodoPago.toUpperCase() === "EFECTIVO"
                ? montoRecibido
                : 0,

        observacion:
            null,

        numeroOperacion:
            null,

        detalles:
            carritoVenta.map(
                function (item) {

                    return {
                        idProducto:
                            Number(
                                item.idProducto
                            ),

                        cantidad:
                            Number(
                                item.cantidad
                            ),

                        precioUnitario:
                            Number(
                                item.precioUnitario
                            ),

                        descuento:
                            Number(
                                item.descuentoPorcentaje || 0
                            )
                    };
                }
            )
    };


    const token =
        document.querySelector(
            '#formTokenNuevaVenta input[name="__RequestVerificationToken"]'
        )
            ?.value
        || "";


    try {

        boton.disabled =
            true;


        boton.innerHTML = `
            <span uk-spinner="ratio: .7"></span>
            Procesando venta...
        `;


        const response =
            await fetch(
                `${window.location.pathname}?handler=ConfirmarVenta`,
                {
                    method:
                        "POST",

                    headers:
                    {
                        "Content-Type":
                            "application/json",

                        "Accept":
                            "application/json",

                        "RequestVerificationToken":
                            token
                    },

                    credentials:
                        "same-origin",

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


        try {

            resultado =
                texto
                    ? JSON.parse(
                        texto
                    )
                    : null;

        }
        catch {

            console.error(
                "[KerniX] Confirmar venta - respuesta no JSON:",
                texto
            );


            throw new Error(
                "El servidor devolvió una respuesta inválida."
            );
        }


        console.log(
            "[KerniX] Confirmar venta respuesta:",
            resultado
        );


        if (!response.ok) {

            throw new Error(
                resultado?.message ||
                `HTTP ${response.status}`
            );
        }


        if (
            !resultado ||
            resultado.success !== true
        ) {

            throw new Error(
                resultado?.message ||
                "No se pudo registrar la venta."
            );
        }


        const data =
            resultado.data || {};


        ultimaVentaRegistrada =
            data.venta || null;


        ultimoComprobanteEmitido =
            data.comprobante || null;


        mostrarAvisoVenta(
            resultado.message ||
            "Venta registrada correctamente.",
            "success"
        );


        if (
            ultimoComprobanteEmitido
        ) {

            mostrarComprobanteEmitido(
                ultimoComprobanteEmitido,
                tipoComprobante
            );

        }
        else {

            ocultarComprobanteEmitido();
        }


        boton.disabled =
            true;


        boton.innerHTML = `
            <span uk-icon="icon: check"></span>
            Venta registrada
        `;


        if (window.UIkit) {

            UIkit.icon(
                boton
            );
        }

    }
    catch (error) {

        console.error(
            "[KerniX] Error confirmarVentaServidor:",
            error
        );


        mostrarAvisoVenta(
            error.message ||
            "No se pudo registrar la venta.",
            "danger"
        );


        boton.disabled =
            false;


        boton.innerHTML = `
            <span uk-icon="icon: check"></span>
            Confirmar venta
        `;


        if (window.UIkit) {

            UIkit.icon(
                boton
            );
        }
    }
}


/* ==========================================================
   MOSTRAR COMPROBANTE EMITIDO
   ========================================================== */

function mostrarComprobanteEmitido(
    comprobante,
    tipoComprobante
) {

    const contenedor =
        document.getElementById(
            "comprobanteEmitido"
        );


    const numero =
        document.getElementById(
            "comprobanteEmitidoNumero"
        );


    const tipo =
        document.getElementById(
            "comprobanteEmitidoTipo"
        );


    const botonEnviar =
        document.getElementById(
            "enviarBoleta"
        );


    if (!contenedor) {
        return;
    }


    const serie =
        String(
            comprobante?.serie || ""
        );


    const correlativo =
        String(
            comprobante?.correlativo || ""
        );


    let numeroCompleto =
        String(
            comprobante?.numeroComprobante ||
            comprobante?.numero ||
            ""
        );


    if (
        !numeroCompleto
        &&
        serie
        &&
        correlativo
    ) {

        numeroCompleto =
            `${serie}-${correlativo}`;
    }


    if (numero) {

        numero.textContent =
            numeroCompleto ||
            "Comprobante generado";
    }


    if (tipo) {

        tipo.textContent =
            obtenerNombreTipoComprobante(
                tipoComprobante
            );
    }


    contenedor.hidden =
        false;


    if (botonEnviar) {

        if (
            tipoComprobante ===
            "BOLETA"
        ) {

            botonEnviar.hidden =
                false;


            botonEnviar.innerHTML = `
                <span uk-icon="icon: copy"></span>
                Copiar datos de boleta
            `;

        }
        else if (
            tipoComprobante ===
            "FACTURA"
        ) {

            botonEnviar.hidden =
                false;


            botonEnviar.innerHTML = `
                <span uk-icon="icon: copy"></span>
                Copiar datos de factura
            `;

        }
        else {

            botonEnviar.hidden =
                true;
        }
    }


    if (window.UIkit) {

        UIkit.icon(
            contenedor
        );


        if (botonEnviar) {

            UIkit.icon(
                botonEnviar
            );
        }
    }
}


/* ==========================================================
   OCULTAR COMPROBANTE
   ========================================================== */

function ocultarComprobanteEmitido() {

    const contenedor =
        document.getElementById(
            "comprobanteEmitido"
        );


    const botonEnviar =
        document.getElementById(
            "enviarBoleta"
        );


    if (contenedor) {

        contenedor.hidden =
            true;
    }


    if (botonEnviar) {

        botonEnviar.hidden =
            true;
    }


    ultimoComprobanteEmitido =
        null;
}


/* ==========================================================
   NOMBRE DEL COMPROBANTE
   ========================================================== */

function obtenerNombreTipoComprobante(
    tipo
) {

    const valor =
        String(
            tipo || ""
        )
            .trim()
            .toUpperCase();


    switch (valor) {

        case "BOLETA":
            return "Boleta";

        case "FACTURA":
            return "Factura";

        case "NOTA_VENTA":
            return "Nota de venta";

        case "SIN_COMPROBANTE":
            return "Sin comprobante";

        default:
            return "Comprobante";
    }
}


/* ==========================================================
   ENVIAR BOLETA / FACTURA
   ========================================================== */

async function enviarBoletaCliente() {
    if (!ultimaVentaRegistrada || !ultimoComprobanteEmitido) {
        mostrarAvisoVenta("Primero registra una venta y genera su comprobante.", "warning");
        return;
    }
    const tipo = document.getElementById("comprobanteEmitidoTipo")?.textContent?.trim() || "Comprobante";
    const numero = document.getElementById("comprobanteEmitidoNumero")?.textContent?.trim() || "Sin número";
    const texto = `${tipo}: ${numero}`;
    try {
        if (!navigator.clipboard?.writeText) throw new Error("Portapapeles no disponible");
        await navigator.clipboard.writeText(texto);
        mostrarAvisoVenta("Datos del comprobante copiados. El envío por correo requiere un endpoint en el servidor.", "success");
    } catch {
        mostrarAvisoVenta(`Comprobante: ${texto}. Copia este dato manualmente; el envío por correo no está implementado.`, "primary");
    }
}

/* ==========================================================
   LIMPIAR VENTA COMPLETA
   ========================================================== */
function limpiarVentaCompleta() {

    carritoVenta.splice(
        0,
        carritoVenta.length
    );


    limpiarClienteSeleccionado();


    const buscarProducto =
        document.getElementById(
            "buscarProducto"
        );


    if (buscarProducto) {

        buscarProducto.value =
            "";
    }


    mostrarMensajeProductos(
        "Escribe al menos 2 caracteres para buscar productos."
    );


    const montoRecibido =
        document.getElementById(
            "montoRecibido"
        );


    if (montoRecibido) {

        montoRecibido.value =
            "0";
    }


    const metodoEfectivo =
        document.querySelector(
            'input[name="MetodoPago"][value="Efectivo"]'
        );


    if (metodoEfectivo) {

        metodoEfectivo.checked =
            true;
    }


    const comprobanteBoleta =
        document.querySelector(
            'input[name="TipoComprobante"][value="BOLETA"]'
        );


    if (comprobanteBoleta) {

        comprobanteBoleta.checked =
            true;
    }


    ultimaVentaRegistrada =
        null;


    ocultarComprobanteEmitido();


    const botonConfirmar =
        document.getElementById(
            "confirmarVenta"
        );


    if (botonConfirmar) {

        botonConfirmar.disabled =
            false;


        botonConfirmar.innerHTML = `
            <span uk-icon="icon: check"></span>
            Confirmar venta
        `;
    }


    renderizarCarrito();


    recalcularVenta();


    if (window.UIkit) {

        UIkit.icon(
            document.body
        );
    }


    mostrarAvisoVenta(
        "La venta fue limpiada.",
        "primary"
    );
}


/* ==========================================================
   AVISO / NOTIFICACIÓN
   ========================================================== */
function mostrarAvisoVenta(
    mensaje,
    estado = "primary"
) {

    const anterior =
        document.getElementById(
            "kxToastVenta"
        );


    if (anterior) {

        anterior.remove();
    }


    const toast =
        document.createElement(
            "div"
        );


    toast.id =
        "kxToastVenta";


    toast.textContent =
        mensaje;


    toast.style.position =
        "fixed";

    toast.style.top =
        "24px";

    toast.style.right =
        "24px";

    toast.style.zIndex =
        "99999999";

    toast.style.minWidth =
        "300px";

    toast.style.maxWidth =
        "450px";

    toast.style.padding =
        "16px 20px";

    toast.style.borderRadius =
        "12px";

    toast.style.fontWeight =
        "700";

    toast.style.fontSize =
        "14px";

    toast.style.boxShadow =
        "0 15px 40px rgba(0,0,0,.45)";

    toast.style.color =
        "#ffffff";


    if (estado === "success") {

        toast.style.background =
            "#16835d";

    }
    else if (estado === "danger") {

        toast.style.background =
            "#b83248";

    }
    else if (estado === "warning") {

        toast.style.background =
            "#b7791f";

    }
    else {

        toast.style.background =
            "#1d4f7a";
    }


    document.body.appendChild(
        toast
    );


    setTimeout(
        function () {

            toast.remove();

        },
        3500
    );
}

/* ==========================================================
   EVENTOS DE COMPROBANTE
   ========================================================== */

function iniciarEventosComprobante() {

    const radios =
        document.querySelectorAll(
            'input[name="TipoComprobante"]'
        );


    radios.forEach(
        function (radio) {

            radio.addEventListener(
                "change",
                function () {

                    const tipo =
                        obtenerTipoComprobanteSeleccionado();


                    const campo =
                        document.getElementById(
                            "comprobanteCliente"
                        );


                    if (campo) {

                        campo.value =
                            tipo;
                    }


                    console.log(
                        "[KerniX] Comprobante seleccionado:",
                        tipo
                    );
                }
            );
        }
    );
}


/* ==========================================================
   EVENTOS GENERALES DE VENTA
   ========================================================== */

function iniciarEventosVenta() {

    const limpiar =
        document.getElementById(
            "limpiarVenta"
        );


    const confirmar =
        document.getElementById(
            "confirmarVenta"
        );


    const enviar =
        document.getElementById(
            "enviarBoleta"
        );


    if (limpiar) {

        limpiar.addEventListener(
            "click",
            function () {

                limpiarVentaCompleta();
            }
        );
    }
    else {

        console.warn(
            "[KerniX] No existe #limpiarVenta"
        );
    }


    if (confirmar) {

        confirmar.addEventListener(
            "click",
            function () {

                confirmarVentaServidor();
            }
        );
    }
    else {

        console.warn(
            "[KerniX] No existe #confirmarVenta"
        );
    }


    if (enviar) {

        enviar.addEventListener(
            "click",
            function () {

                enviarBoletaCliente();
            }
        );
    }
    else {

        console.warn(
            "[KerniX] No existe #enviarBoleta"
        );
    }
}


/* ==========================================================
   INICIALIZACIÓN GENERAL
   ========================================================== */

document.addEventListener(
    "DOMContentLoaded",
    function () {

        console.log(
            "[KerniX] Inicializando Nueva Venta..."
        );


        try {

            iniciarBuscadorClientes();


            iniciarBuscadorProductos();


            iniciarModalNuevoCliente();
            document.addEventListener("keydown", event => {
                if (event.key === "Escape" && !document.getElementById("modalNuevoCliente")?.hidden) cerrarModalNuevoCliente();
            });


            iniciarEventosPago();


            iniciarEventosComprobante();


            iniciarEventosVenta();


            renderizarCarrito();


            ocultarComprobanteEmitido();


            console.log(
                "[KerniX] Nueva Venta inicializada correctamente."
            );

        }
        catch (error) {

            console.error(
                "[KerniX] Error inicializando Nueva Venta:",
                error
            );
        }
    }
);
