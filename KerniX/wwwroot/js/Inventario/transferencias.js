document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (event) {
        const button = event.target.closest("[data-confirm]");

        if (!button) {
            return;
        }

        const message =
            button.getAttribute("data-confirm")
            || "¿Confirmar la operación?";

        if (!window.confirm(message)) {
            event.preventDefault();
        }
    });
});
