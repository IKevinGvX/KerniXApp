(() => {
    "use strict";

    const almacen =
        document.getElementById("idAlmacen");

    const producto =
        document.getElementById("idProducto");

    const cantidad =
        document.getElementById("cantidad");

    const stockActual =
        document.getElementById("stockActual");

    const stockReservado =
        document.getElementById("stockReservado");

    const stockDisponible =
        document.getElementById("stockDisponible");

    const stockPosterior =
        document.getElementById("stockPosterior");

    const flujoAnterior =
        document.getElementById("flujoAnterior");

    const flujoSalida =
        document.getElementById("flujoSalida");

    const flujoNuevo =
        document.getElementById("flujoNuevo");

    const mensaje =
        document.getElementById("stockMensaje");


    if (
        !almacen
        ||
        !producto
        ||
        !cantidad
    ) {
        return;
    }


    let disponibleActual =
        null;


    function numero(valor) {
        const n =
            Number(valor);

        return Number.isFinite(n)
            ? n
            : 0;
    }


    function formato(valor) {
        return new Intl.NumberFormat(
            "es-PE",
            {
                minimumFractionDigits: 0,
                maximumFractionDigits: 4
            }
        )
            .format(
                numero(valor)
            );
    }


    function resetStock() {
        disponibleActual =
            null;

        stockActual.textContent =
            "—";

        stockReservado.textContent =
            "—";

        stockDisponible.textContent =
            "—";

        stockPosterior.textContent =
            "—";

        flujoAnterior.textContent =
            "—";

        flujoNuevo.textContent =
            "—";

        mensaje.className =
            "kx-stock-message";

        mensaje.textContent =
            "Selecciona almacén y producto para consultar el stock real.";
    }


    function actualizarCalculo() {
        const salida =
            Math.max(
                0,
                numero(
                    cantidad.value
                )
            );


        flujoSalida.textContent =
            `-${formato(salida)}`;


        if (disponibleActual === null) {
            stockPosterior.textContent =
                "—";

            flujoNuevo.textContent =
                "—";

            return;
        }


        const nuevo =
            disponibleActual
            -
            salida;


        stockPosterior.textContent =
            formato(
                nuevo
            );

        flujoAnterior.textContent =
            formato(
                disponibleActual
            );

        flujoNuevo.textContent =
            formato(
                nuevo
            );


        if (salida <= 0) {
            mensaje.className =
                "kx-stock-message error";

            mensaje.textContent =
                "La cantidad debe ser mayor que cero.";

            return;
        }


        if (salida > disponibleActual) {
            mensaje.className =
                "kx-stock-message error";

            mensaje.textContent =
                `Stock insuficiente. Disponible: ${formato(disponibleActual)}.`;

            return;
        }


        mensaje.className =
            "kx-stock-message ok";

        mensaje.textContent =
            `Operación válida. Stock disponible después de la salida: ${formato(nuevo)}.`;
    }


    async function consultarStock() {
        const idAlmacen =
            Number(
                almacen.value
            );

        const idProducto =
            Number(
                producto.value
            );


        if (
            idAlmacen <= 0
            ||
            idProducto <= 0
        ) {
            resetStock();
            return;
        }


        mensaje.className =
            "kx-stock-message";

        mensaje.textContent =
            "Consultando stock...";


        try {
            const url =
                `/Inventario/Salidas/Nueva?handler=Stock&idAlmacen=${encodeURIComponent(idAlmacen)}&idProducto=${encodeURIComponent(idProducto)}`;


            const response =
                await fetch(
                    url,
                    {
                        method: "GET",
                        credentials: "same-origin",
                        headers: {
                            "Accept": "application/json"
                        }
                    }
                );


            const payload =
                await response.json();


            if (
                !response.ok
                ||
                !payload?.success
            ) {
                throw new Error(
                    payload?.message
                    ||
                    "No se pudo consultar el stock."
                );
            }


            const data =
                payload.data;


            stockActual.textContent =
                formato(
                    data.stockActual
                );

            stockReservado.textContent =
                formato(
                    data.stockReservado
                );

            stockDisponible.textContent =
                formato(
                    data.stockDisponible
                );


            disponibleActual =
                numero(
                    data.stockDisponible
                );


            cantidad.max =
                disponibleActual > 0
                    ? String(
                        disponibleActual
                    )
                    : "0";


            actualizarCalculo();
        }
        catch (error) {
            resetStock();

            mensaje.className =
                "kx-stock-message error";

            mensaje.textContent =
                error instanceof Error
                    ? error.message
                    : "No se pudo consultar el stock.";
        }
    }


    almacen.addEventListener(
        "change",
        consultarStock
    );


    producto.addEventListener(
        "change",
        consultarStock
    );


    cantidad.addEventListener(
        "input",
        actualizarCalculo
    );


    actualizarCalculo();


    if (
        Number(almacen.value) > 0
        &&
        Number(producto.value) > 0
    ) {
        consultarStock();
    }
})();
