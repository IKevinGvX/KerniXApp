(() => {
    'use strict';
    const root = document.getElementById('kacApp');
    if (!root) return;
    const form = document.getElementById('kacAiForm');
    const input = document.getElementById('kacAiPrompt');
    const button = document.getElementById('kacAiEnviar');
    const history = document.getElementById('kacAiMessages');
    const state = document.getElementById('kacAiEstado');
    if (!form || !input || !button || !history || !state) return;

    let sending = false;
    function addMessage(text, side) {
        const bubble = document.createElement('div');
        bubble.className = `kac-bubble kac-${side}`;
        bubble.textContent = text; // No se interpreta HTML generado por el modelo.
        history.appendChild(bubble);
        while (history.children.length > 24) history.removeChild(history.firstElementChild);
        history.scrollTop = history.scrollHeight;
        return bubble;
    }

    document.querySelectorAll('[data-kac-prompt]').forEach(buttonSuggestion => {
        buttonSuggestion.addEventListener('click', () => {
            if (sending || input.disabled) return;
            input.value = buttonSuggestion.getAttribute('data-kac-prompt') || '';
            input.focus();
            form.requestSubmit();
        });
    });

    form.addEventListener('submit', async (ev) => {
        ev.preventDefault();
        if (sending) return;
        const mensaje = input.value.trim();
        if (mensaje.length < 2 || mensaje.length > 1500) {
            state.textContent = 'El mensaje debe tener entre 2 y 1500 caracteres.';
            return;
        }
        sending = true;
        button.disabled = true;
        input.disabled = true;
        addMessage(mensaje, 'user');
        const placeholder = addMessage('KerniX IA está generando la respuesta...', 'assistant');
        state.textContent = 'Consultando llama3.2 mediante KerniX.IA.Api...';

        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const body = new URLSearchParams({ mensaje, __RequestVerificationToken: token });
        try {
            const response = await fetch(form.action, {
                method: 'POST', credentials: 'same-origin',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8'
                },
                body
            });
            const json = await response.json();
            if (!response.ok || !json.ok) {
                throw new Error(json.mensaje || `Error HTTP ${response.status}`);
            }
            placeholder.textContent = json.respuesta || 'La IA devolvió una respuesta vacía.';
            if (json.consultoCatalogo === true) {
                window.KerniXCatalogoUI?.cargarDesdeChat(json.items, json.sugerenciaBorrador);
                state.textContent = `Datos reales verificados en KerniX SQL · ${Array.isArray(json.items) ? json.items.length : 0} productos. Sin escrituras.`;
            } else {
                state.textContent = `Respondió ${json.proveedor || 'Ollama'} · ${json.modelo || 'modelo local'}. No se ejecutaron operaciones comerciales.`;
            }
            input.value = '';
        } catch (error) {
            placeholder.textContent = 'No se pudo generar la respuesta: ' + (error?.message || 'Error de comunicación.');
            state.textContent = 'Comprueba que KerniX Web y KerniX.IA.Api estén ejecutándose.';
        } finally {
            sending = false;
            input.disabled = false;
            button.disabled = false;
            input.focus();
            history.scrollTop = history.scrollHeight;
        }
    });
})();
