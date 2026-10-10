(function () {
    'use strict';
    document.querySelectorAll('.pv-state-form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            var mensaje = form.getAttribute('data-confirm') || '¿Confirmar operación?';
            if (!window.confirm(mensaje)) event.preventDefault();
        });
    });
    document.querySelectorAll('[data-normalize-ruc]').forEach(function (campo) {
        campo.addEventListener('input', function () {
            campo.value = campo.value.replace(/\D/g, '').slice(0, 11);
        });
    });
    document.querySelectorAll('.pv-form-main').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (event.defaultPrevented || !form.checkValidity()) return;
            var boton = form.querySelector('[data-submit-button]');
            if (!boton) return;
            // El botón solo se desactiva luego de la validación nativa del navegador.
            boton.disabled = true;
            boton.textContent = 'Procesando...';
        });
    });
})();
