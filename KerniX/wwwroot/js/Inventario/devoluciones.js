document.addEventListener("DOMContentLoaded", function () {

    const tipo =
        document.getElementById("kdevTipo");

    const message =
        document.getElementById("kdevDirectionMessage");

    function refreshDirection() {

        if (!tipo || !message) {
            return;
        }

        const supplier =
            tipo.value === "proveedor";

        message.classList.toggle(
            "customer",
            !supplier
        );

        message.classList.toggle(
            "supplier",
            supplier
        );

        message.innerHTML =
            supplier
                ? "<strong>Proveedor → egreso:</strong> esta operación descontará stock del almacén."
                : "<strong>Cliente → ingreso:</strong> esta operación incrementará el stock del almacén.";
    }

    if (tipo) {
        tipo.addEventListener(
            "change",
            refreshDirection
        );

        refreshDirection();
    }


    document.addEventListener(
        "click",
        function (event) {

            const button =
                event.target.closest(
                    "[data-confirm]"
                );

            if (!button) {
                return;
            }

            const text =
                button.getAttribute(
                    "data-confirm"
                )
                || "¿Confirmar la operación?";

            if (!window.confirm(text)) {
                event.preventDefault();
            }
        }
    );

});
