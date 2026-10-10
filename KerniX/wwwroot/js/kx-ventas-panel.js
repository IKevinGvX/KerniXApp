(() => {
    'use strict';
    const $ = (sel, scope = document) => scope.querySelector(sel);
    const list = (sel, scope = document) => [...scope.querySelectorAll(sel)];
    const input = $('#buscarGeneral');
    document.addEventListener('keydown', e => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            if (!input) return;
            e.preventDefault();
            input.focus();
            input.select();
        }
        if (e.key === 'Escape') $('#menuLateral')?.classList.remove('kx-open');
    });
    $('[data-menu-toggle]')?.addEventListener('click', () => $('#menuLateral')?.classList.toggle('kx-open'));
    document.addEventListener('click', e => {
        const side = $('#menuLateral');
        if (side?.classList.contains('kx-open') && !e.target.closest('#menuLateral') && !e.target.closest('[data-menu-toggle]'))
            side.classList.remove('kx-open');
    });
    const dlg = $('#detalleKx');
    const campos = $('#detalleKxCampos');
    const keys = [['id', 'Identificador'], ['doc', 'Documento'], ['cliente', 'Cliente'], ['fecha', 'Fecha'], ['tipo', 'Tipo / método'], ['monto', 'Monto (S/)'], ['estado', 'Estado'], ['venta', 'Número de venta']];
    list('[data-ver]').forEach(btn => btn.addEventListener('click', () => {
        if (!dlg || !campos) return;
        campos.replaceChildren();
        for (const [key, label] of keys) {
            const div = document.createElement('div');
            const dt = document.createElement('dt'); dt.textContent = label;
            const dd = document.createElement('dd'); dd.textContent = btn.dataset[key] || '—';
            div.append(dt, dd);
            campos.append(div);
        }
        if (typeof dlg.showModal === 'function') dlg.showModal();
        else dlg.setAttribute('open', '');
    }));
    list('[data-cerrar]').forEach(btn => btn.addEventListener('click', () => {
        if (typeof dlg?.close === 'function') dlg.close();
        else dlg?.removeAttribute('open');
    }));
    dlg?.addEventListener('click', e => { if (e.target === dlg) dlg.close(); });
    const form = $('#busquedaPanel');
    form?.addEventListener('submit', e => {
        const desde = $('[name="desde"]', form)?.value;
        const hasta = $('[name="hasta"]', form)?.value;
        if (desde && hasta && desde > hasta) {
            e.preventDefault();
            window.alert('La fecha inicial no puede ser posterior a la fecha final.');
        }
    });
})();
