(() => {
    'use strict';
    const form = document.getElementById('kaiAgentForm');
    if (!form) return;
    const byId = (id) => document.getElementById(id);
    const error = byId('kaiFormError');
    const panel = byId('kaiDraftPanel');
    const draft = byId('kaiDraftText');
    const copied = byId('kaiCopyResult');

    form.addEventListener('submit', (ev) => {
        ev.preventDefault();
        const cliente = byId('kaiCliente').value.trim();
        const producto = byId('kaiProducto').value.trim();
        const cantidad = Number(byId('kaiCantidad').value);
        const tipo = byId('kaiTipo').value;
        const notas = byId('kaiNotas').value.trim();
        error.hidden = true;
        if (!cliente || !producto || !Number.isSafeInteger(cantidad) || cantidad < 1 || cantidad > 999999) {
            error.textContent = 'Completa el cliente, el producto y una cantidad entera válida.';
            error.hidden = false;
            return;
        }
        const contenido = [
            'KERNIX ERP — SOLICITUD COMERCIAL (BORRADOR)',
            'Cliente: ' + cliente,
            'Operación solicitada: ' + tipo,
            'Producto o necesidad: ' + producto,
            'Cantidad solicitada: ' + cantidad,
            'Observaciones: ' + (notas || 'No indicadas'),
            '',
            'ESTADO: PENDIENTE DE VERIFICAR PRECIOS Y STOCK EN API.',
            'NO es una cotización, venta ni pedido registrado.',
            'Requiere autorización antes de efectuar transacciones.'
        ].join('\n');
        draft.textContent = contenido;
        panel.hidden = false;
        copied.textContent = '';
        panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    form.addEventListener('reset', () => {
        panel.hidden = true;
        error.hidden = true;
        copied.textContent = '';
    });

    byId('kaiCopyDraft')?.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(draft.textContent || '');
            copied.textContent = 'Borrador copiado';
        } catch {
            copied.textContent = 'No se pudo copiar automáticamente; selecciona el texto del borrador.';
        }
    });
})();
