/* KerniX ERP — UX de recuperación. El servidor valida todas las condiciones. */
(() => {
    'use strict';
    const $ = (selector) => document.querySelector(selector);
    const utf8Bytes = (value) => new TextEncoder().encode(value).length;

    // No mantener el token secreto en el historial visible ni exponerlo mediante Referer.
    if (location.pathname.toLowerCase().endsWith('/account/resetpassword') &&
        location.search && window.history?.replaceState) {
        window.history.replaceState(null, document.title, location.pathname);
    }

    document.querySelectorAll('[data-toggle-password]').forEach((button) => {
        button.addEventListener('click', () => {
            const target = document.getElementById(button.dataset.togglePassword);
            if (!target) return;
            const visible = target.type === 'text';
            target.type = visible ? 'password' : 'text';
            button.setAttribute('aria-pressed', String(!visible));
            button.setAttribute('aria-label', visible ? 'Mostrar contraseña' : 'Ocultar contraseña');
        });
    });

    const passwordInput = $('#kxrp-password');
    const strength = $('.kxrp-strength');
    const strengthLabel = $('#kxrp-strength-label');

    function updateStrength() {
        if (!passwordInput || !strength || !strengthLabel) return;
        const value = passwordInput.value;
        if (!value) {
            strength.dataset.score = '0';
            strengthLabel.textContent = 'Seguridad: escribe una contraseña';
            return;
        }
        let score = 0;
        if (value.length >= 12) score++;
        if (value.length >= 16) score++;
        if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score++;
        if (/\d/.test(value) && /[^a-zA-Z\d]/.test(value)) score++;
        score = Math.max(1, score);
        const names = { 1: 'Básica', 2: 'Media', 3: 'Alta', 4: 'Muy alta' };
        strength.dataset.score = String(score);
        strengthLabel.textContent = `Seguridad estimada: ${names[score]} · ${utf8Bytes(value)}/72 bytes`;
    }
    if (passwordInput) {
        passwordInput.addEventListener('input', updateStrength);
        updateStrength();
    }

    const forgotForm = $('#kxrp-forgot-form');
    const resetForm = $('#kxrp-reset-form');
    const submitButton = $('#kxrp-submit');

    function activateLoading() {
        if (!submitButton) return;
        submitButton.disabled = true;
        const label = submitButton.querySelector('[data-submit-label]');
        if (label) label.textContent = 'Procesando de forma segura…';
    }

    if (forgotForm) {
        forgotForm.addEventListener('submit', (event) => {
            if (!forgotForm.checkValidity()) return;
            activateLoading();
        });
    }

    if (resetForm) {
        resetForm.addEventListener('submit', (event) => {
            const password = $('#kxrp-password')?.value || '';
            const confirmation = $('#kxrp-confirm')?.value || '';
            const error = $('#kxrp-client-error');
            if (error) { error.hidden = true; error.textContent = ''; }
            const fail = (message) => {
                event.preventDefault();
                if (error) { error.textContent = message; error.hidden = false; }
            };
            if (password !== confirmation) return fail('Las contraseñas deben coincidir.');
            if (password.length < 12) return fail('Utiliza al menos 12 caracteres.');
            if (utf8Bytes(password) > 72) return fail('La contraseña supera el máximo de 72 bytes UTF-8.');
            if (!resetForm.checkValidity()) return;
            activateLoading();
        });
    }
})();
