/* ==========================================================
   KERNIX
   PRECARGA PEDIDO -> NUEVA VENTA

   IMPORTANTE:
   - Se carga DESPUÉS de nueva.js
   - No modifica C#
   - No modifica SQL
   - Usa las funciones oficiales de nueva.js
   ========================================================== */

document.addEventListener(
    "DOMContentLoaded",
    async function () {

        console.log(
            "[KerniX Pedido] Iniciando precarga..."
        );

        // ======================================================
        // EVITAR DOBLE PRECARGA
        // ======================================================

        if (window.__kernixPedidoPrecargado === true) {

            console.warn(
                "[KerniX Pedido] El pedido ya fue precargado."
            );

            return;
        }


        // ======================================================
        // OBTENER JSON DEL PEDIDO
        // ======================================================

        const elementoJson =
            document.getElementById(
                "kernixPedidoOrigenJson"
            );


        if (!elementoJson) {

            console.log(
                "[KerniX Pedido] La venta no proviene de un pedido."
            );

            return;
        }


        let pedido = null;


        try {

            const texto =
                elementoJson.textContent
                    ?.trim()
                || "";


            if (!texto) {

                throw new Error(
                    "El JSON del pedido está vacío."
                );
            }


            pedido =
                JSON.parse(
                    texto
                );

        }
        catch (error) {

            console.error(
                "[KerniX Pedido] No se pudo leer el pedido:",
                error
            );


            if (
                typeof mostrarAvisoVenta ===
                "function"
            ) {

                mostrarAvisoVenta(
                    "No se pudo leer la información del pedido.",
                    "danger"
                );
            }


            return;
        }


        if (!pedido) {

            console.error(
                "[KerniX Pedido] Pedido nulo."
            );

            return;
        }


        console.log(
            "[KerniX Pedido] Pedido recibido:",
            pedido
        );


        // ======================================================
        // VALIDAR FUNCIONES DE NUEVA.JS
        // ======================================================

        const funcionesRequeridas =
            [
                [
                    "seleccionarCliente",
                    typeof seleccionarCliente
                ],

                [
                    "agregarProductoCarrito",
                    typeof agregarProductoCarrito
                ],

                [
                    "establecerCantidadProducto",
                    typeof establecerCantidadProducto
                ],

                [
                    "establecerDescuentoProducto",
                    typeof establecerDescuentoProducto
                ],

                [
                    "seleccionarComprobantePorCliente",
                    typeof seleccionarComprobantePorCliente
                ],

                [
                    "mostrarAvisoVenta",
                    typeof mostrarAvisoVenta
                ]
            ];


        const funcionFaltante =
            funcionesRequeridas.find(
                x => x[1] !== "function"
            );


        if (funcionFaltante) {

            console.error(
                "[KerniX Pedido] No existe función:",
                funcionFaltante[0]
            );

            return;
        }


        // ======================================================
        // BLOQUEAR CONFIRMACIÓN DURANTE LA CARGA
        // ======================================================

        const botonConfirmar =
            document.getElementById(
                "confirmarVenta"
            );


        if (botonConfirmar) {

            botonConfirmar.disabled =
                true;

            botonConfirmar.title =
                "Cargando pedido...";
        }


        // ======================================================
        // CLIENTE
        // ======================================================

        const cliente =
            window.kernixClientePedido;


        if (
            !cliente ||
            Number(
                cliente.idCliente || 0
            ) <= 0
        ) {

            console.error(
                "[KerniX Pedido] Cliente inválido:",
                cliente
            );


            mostrarAvisoVenta(
                "No se pudo identificar el cliente del pedido.",
                "danger"
            );


            return;
        }


        seleccionarCliente(
            cliente
        );


        console.log(
            "[KerniX Pedido] Cliente precargado:",
            cliente
        );


        // ======================================================
        // PRODUCTOS DEL PEDIDO
        // ======================================================

        const lineas =
            Array.isArray(
                pedido.productos
            )
                ? pedido.productos
                : [];


        console.log(
            "[KerniX Pedido] Productos recibidos:",
            lineas.length,
            lineas
        );


        if (lineas.length === 0) {

            mostrarAvisoVenta(
                "El pedido no devolvió productos.",
                "warning"
            );


            console.error(
                "[KerniX Pedido] pedido.productos está vacío.",
                pedido
            );


            return;
        }


        let productosAgregados =
            0;


        // ======================================================
        // RECORRER PRODUCTOS
        // ======================================================

        for (
            const linea of lineas
        ) {

            try {

                console.log(
                    "[KerniX Pedido] Procesando línea:",
                    linea
                );


                // ------------------------------------------------
                // PRODUCTO
                // ------------------------------------------------

                const idProducto =
                    Number(
                        linea.idProducto || 0
                    );


                if (
                    !Number.isSafeInteger(
                        idProducto
                    )
                    ||
                    idProducto <= 0
                ) {

                    console.warn(
                        "[KerniX Pedido] IdProducto inválido:",
                        linea
                    );

                    continue;
                }


                // ------------------------------------------------
                // CANTIDADES
                // ------------------------------------------------

                const solicitada =
                    Number(
                        linea.cantidadSolicitada
                        || 0
                    );


                const atendida =
                    Number(
                        linea.cantidadAtendida
                        || 0
                    );


                let cantidadPendiente =
                    linea.cantidadPendiente
                        !== undefined
                        &&
                        linea.cantidadPendiente
                        !== null

                        ? Number(
                            linea.cantidadPendiente
                        )

                        : (
                            solicitada
                            -
                            atendida
                        );


                if (
                    !Number.isFinite(
                        cantidadPendiente
                    )
                ) {

                    cantidadPendiente =
                        0;
                }


                cantidadPendiente =
                    Math.floor(
                        cantidadPendiente
                    );


                console.log(
                    "[KerniX Pedido] Cantidades:",
                    {
                        idProducto,
                        solicitada,
                        atendida,
                        cantidadPendiente
                    }
                );


                if (
                    cantidadPendiente <= 0
                ) {

                    console.warn(
                        "[KerniX Pedido] Producto sin cantidad pendiente:",
                        idProducto
                    );

                    continue;
                }


                // ------------------------------------------------
                // DATOS BASE DESDE EL PEDIDO
                // ------------------------------------------------

                let codigo =
                    String(
                        linea.codigo || ""
                    );


                let nombre =
                    String(
                        linea.producto
                        ||
                        linea.nombre
                        ||
                        `Producto ${idProducto}`
                    );


                let precio =
                    Number(
                        linea.precioUnitario
                        || 0
                    );


                let abreviaturaUnidad =
                    "und";


                let stockDisponible =
                    0;


                let controlaStock =
                    false;


                // =================================================
                // CONSULTAR PRODUCTO
                // =================================================

                try {

                    const urlProducto =
                        `${window.location.pathname}` +
                        `?handler=Producto` +
                        `&idProducto=${encodeURIComponent(
                            idProducto
                        )}`;


                    const respuestaProducto =
                        await fetch(
                            urlProducto,
                            {
                                method:
                                    "GET",

                                credentials:
                                    "same-origin",

                                headers:
                                {
                                    "Accept":
                                        "application/json"
                                }
                            }
                        );


                    const textoProducto =
                        await respuestaProducto
                            .text();


                    let resultadoProducto =
                        null;


                    try {

                        resultadoProducto =
                            textoProducto
                                ? JSON.parse(
                                    textoProducto
                                )
                                : null;

                    }
                    catch {

                        console.warn(
                            "[KerniX Pedido] Producto devolvió respuesta no JSON:",
                            textoProducto
                        );
                    }


                    if (
                        respuestaProducto.ok
                        &&
                        resultadoProducto
                            ?.success === true
                        &&
                        resultadoProducto.data
                    ) {

                        const api =
                            resultadoProducto.data;


                        codigo =
                            api.codigo
                            ||
                            codigo;


                        nombre =
                            api.nombre
                            ||
                            nombre;


                        abreviaturaUnidad =
                            api.abreviaturaUnidad
                            ||
                            abreviaturaUnidad;


                        /*
                         * PRECIO:
                         *
                         * Para una venta proveniente de pedido
                         * respetamos PRIMERO el precio pactado
                         * en el pedido.
                         */
                        if (
                            !Number.isFinite(
                                precio
                            )
                            ||
                            precio <= 0
                        ) {

                            precio =
                                Number(
                                    api.precioVenta
                                    || 0
                                );
                        }


                        if (
                            api.controlaStock ===
                            true
                        ) {

                            controlaStock =
                                true;
                        }


                        if (
                            Number.isFinite(
                                Number(
                                    api.stockDisponible
                                )
                            )
                        ) {

                            stockDisponible =
                                Number(
                                    api.stockDisponible
                                );
                        }

                    }

                }
                catch (error) {

                    console.warn(
                        "[KerniX Pedido] No se pudo consultar información adicional del producto:",
                        idProducto,
                        error
                    );
                }


                // =================================================
                // CONSULTAR STOCK
                // =================================================

                try {

                    const urlStock =
                        `${window.location.pathname}` +
                        `?handler=StockProducto` +
                        `&idProducto=${encodeURIComponent(
                            idProducto
                        )}`;


                    const respuestaStock =
                        await fetch(
                            urlStock,
                            {
                                method:
                                    "GET",

                                credentials:
                                    "same-origin",

                                headers:
                                {
                                    "Accept":
                                        "application/json"
                                }
                            }
                        );


                    const textoStock =
                        await respuestaStock
                            .text();


                    let resultadoStock =
                        null;


                    try {

                        resultadoStock =
                            textoStock
                                ? JSON.parse(
                                    textoStock
                                )
                                : null;

                    }
                    catch {

                        console.warn(
                            "[KerniX Pedido] Stock devolvió respuesta no JSON:",
                            textoStock
                        );
                    }


                    if (
                        respuestaStock.ok
                        &&
                        resultadoStock
                            ?.success === true
                        &&
                        resultadoStock.data
                    ) {

                        const dataStock =
                            resultadoStock.data;


                        stockDisponible =
                            Number(
                                dataStock
                                    .stockDisponible
                                || 0
                            );


                        controlaStock =
                            dataStock
                                .controlaStock ===
                            true;

                    }

                }
                catch (error) {

                    console.warn(
                        "[KerniX Pedido] No se pudo consultar stock:",
                        idProducto,
                        error
                    );
                }


                // =================================================
                // VALIDAR PRECIO
                // =================================================

                if (
                    !Number.isFinite(
                        precio
                    )
                    ||
                    precio <= 0
                ) {

                    console.error(
                        "[KerniX Pedido] Precio inválido:",
                        {
                            idProducto,
                            precio,
                            linea
                        }
                    );


                    mostrarAvisoVenta(
                        `El producto ${nombre} no tiene un precio válido.`,
                        "warning"
                    );


                    continue;
                }


                // =================================================
                // DESCUENTO DEL PEDIDO
                // =================================================

                const descuentoMonto =
                    Number(
                        linea.descuento
                        || 0
                    );


                const brutoOriginal =
                    solicitada
                    *
                    precio;


                let descuentoPorcentaje =
                    brutoOriginal > 0
                        ?
                        (
                            descuentoMonto
                            /
                            brutoOriginal
                        )
                        *
                        100
                        :
                        0;


                if (
                    !Number.isFinite(
                        descuentoPorcentaje
                    )
                    ||
                    descuentoPorcentaje < 0
                ) {

                    descuentoPorcentaje =
                        0;
                }


                // =================================================
                // OBJETO COMPATIBLE CON NUEVA.JS
                // =================================================

                const productoVenta =
                {
                    idProducto:
                        idProducto,

                    codigo:
                        codigo,

                    nombre:
                        nombre,

                    /*
                     * agregarProductoCarrito()
                     * espera precioVenta.
                     */
                    precioVenta:
                        precio,

                    stockDisponible:
                        stockDisponible,

                    controlaStock:
                        controlaStock,

                    abreviaturaUnidad:
                        abreviaturaUnidad
                };


                console.log(
                    "[KerniX Pedido] Enviando al carrito:",
                    productoVenta
                );


                // =================================================
                // AGREGAR USANDO NUEVA.JS
                // =================================================

                agregarProductoCarrito(
                    productoVenta
                );


                /*
                 * agregarProductoCarrito agrega 1.
                 * Ahora establecemos la cantidad exacta
                 * pendiente del pedido.
                 */
                establecerCantidadProducto(
                    idProducto,
                    cantidadPendiente
                );


                /*
                 * Aplicar descuento original como porcentaje
                 * para que nueva.js pueda calcular la línea.
                 */
                establecerDescuentoProducto(
                    idProducto,
                    descuentoPorcentaje
                );


                // =================================================
                // CONFIRMAR QUE APARECIÓ EN LA TABLA
                // =================================================

                const productoEnPantalla =
                    document.querySelector(
                        `.kxquitarproducto[data-id="${idProducto}"]`
                    );


                if (
                    productoEnPantalla
                ) {

                    productosAgregados++;


                    console.log(
                        "[KerniX Pedido] Producto precargado correctamente:",
                        {
                            idProducto,
                            nombre,
                            cantidadPendiente,
                            precio,
                            stockDisponible
                        }
                    );

                }
                else {

                    console.warn(
                        "[KerniX Pedido] El producto no fue agregado al carrito:",
                        {
                            idProducto,
                            nombre,
                            stockDisponible,
                            controlaStock
                        }
                    );
                }

            }
            catch (error) {

                console.error(
                    "[KerniX Pedido] Error procesando una línea:",
                    linea,
                    error
                );
            }
        }


        // ======================================================
        // RESULTADO
        // ======================================================

        if (
            productosAgregados <= 0
        ) {

            console.error(
                "[KerniX Pedido] Ningún producto pudo precargarse."
            );


            mostrarAvisoVenta(
                "El pedido fue cargado, pero sus productos no pudieron agregarse a la venta. Revisa la consola.",
                "danger"
            );


            return;
        }


        // ======================================================
        // BLOQUEAR CAMBIO DE CLIENTE
        // ======================================================

        const quitarCliente =
            document.getElementById(
                "quitarCliente"
            );


        if (quitarCliente) {

            quitarCliente.disabled =
                true;

            quitarCliente.title =
                "El cliente pertenece al pedido de origen.";
        }


        // ======================================================
        // COMPROBANTE
        // ======================================================

        seleccionarComprobantePorCliente(
            cliente.tipoDocumento,
            cliente.numeroDocumento
        );


        // ======================================================
        // ACTIVAR CONFIRMACIÓN
        // ======================================================

        if (botonConfirmar) {

            botonConfirmar.disabled =
                false;

            botonConfirmar.title =
                "Confirmar venta";
        }


        window.__kernixPedidoPrecargado =
            true;


        // ======================================================
        // MENSAJE FINAL
        // ======================================================

        mostrarAvisoVenta(
            `Pedido ${pedido.numeroPedido
            ||
            pedido.idPedido
            } cargado con ${productosAgregados
            } producto(s).`,
            "success"
        );


        console.log(
            "[KerniX Pedido] PRECARGA COMPLETADA.",
            {
                idPedido:
                    pedido.idPedido,

                numeroPedido:
                    pedido.numeroPedido,

                productosRecibidos:
                    lineas.length,

                productosAgregados:
                    productosAgregados
            }
        );

    }
);