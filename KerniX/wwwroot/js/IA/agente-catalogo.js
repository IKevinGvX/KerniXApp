(() => {
    'use strict';

    const app = document.getElementById('kacApp');
    if (!app) return;
    const el = (id) => document.getElementById(id);
    const form = el('kacSearchForm');
    const messages = el('kacMessages');
    const results = el('kacResultList');
    const count = el('kacResultCount');
    const cart = el('kacCart');
    const total = el('kacTotal');
    const copy = el('kacCopy');
    const validate = el('kacValidar');
    const review = el('kacRevision');
    const clear = el('kacClear');
    const status = el('kacCopyStatus');
    const button = el('kacSearchButton');
    const money = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' });

    // Solo estado temporal del navegador. No es una cotización registrada.
    const selected = new Map();
    let found = [];
    let busy = false;
    let validating = false;
    let verified = null; // Solo el resultado del handler servidor permite copiar.

    const invalidate = () => {
        verified = null;
        if (copy) copy.disabled = true;
        if (review) {
            review.className = 'kac-review';
            review.textContent = 'Borrador cambiado o sin verificar. Valida en SQL antes de copiarlo.';
        }
    };

    const node = (tag, cls = '', text = '') => {
        const element = document.createElement(tag);
        if (cls) element.className = cls;
        if (text !== '') element.textContent = text;
        return element;
    };
    const val = (product, key) => product[key] ?? product[key.charAt(0).toUpperCase() + key.slice(1)];
    const num = (value) => value === null || value === undefined || value === '' ? null : Number(value);

    const bubble = (text, side = 'assistant') => {
        messages.appendChild(node('div', `kac-bubble kac-${side}`, text));
        messages.scrollTop = messages.scrollHeight;
    };

    const toItem = (p) => ({
        id: Number(val(p, 'idProducto')),
        codigo: String(val(p, 'codigo') ?? ''),
        nombre: String(val(p, 'nombre') ?? ''),
        descripcion: String(val(p, 'descripcion') ?? ''),
        precio: num(val(p, 'precioVenta')),
        stock: num(val(p, 'stockDisponible'))
    });

    const renderResults = () => {
        results.replaceChildren();
        count.textContent = `${found.length} resultado${found.length === 1 ? '' : 's'}`;
        if (!found.length) {
            results.appendChild(node('div', 'kac-empty kac-empty-full', 'No se encontraron productos para mostrar. Prueba con otro nombre o código.'));
            return;
        }
        found.forEach((p) => {
            const panel = node('article', 'kac-item');
            const heading = node('div', 'kac-item-head');
            const icon = node('span', 'kac-item-icon');
            const i = node('span');
            i.setAttribute('uk-icon', 'icon: tag');
            icon.appendChild(i);
            const title = node('div');
            title.append(node('h3', 'kac-item-name', p.nombre), node('p', 'kac-item-code', p.codigo || `Producto #${p.id}`));
            if (p.descripcion) title.appendChild(node('p', 'kac-item-description', p.descripcion.slice(0, 180)));
            heading.append(icon, title);

            const bottom = node('div', 'kac-item-foot');
            const figures = node('div');
            figures.appendChild(node('div', 'kac-item-price', p.precio === null ? 'Precio no disponible' : money.format(p.precio)));
            const stLabel = p.stock === null ? 'Stock no verificado' : (p.stock <= 0 ? 'Sin unidades disponibles' : `${p.stock.toLocaleString('es-PE')} disponibles`);
            figures.appendChild(node('div', p.stock === null ? 'kac-item-stock kac-stock-unknown' : p.stock <= 0 ? 'kac-item-stock kac-stock-zero' : 'kac-item-stock', stLabel));
            const add = node('button', 'kac-add', 'Añadir');
            add.type = 'button';
            add.disabled = !Number.isSafeInteger(p.id) || p.id <= 0 || (p.stock !== null && p.stock < 1);
            add.setAttribute('aria-label', `Añadir ${p.nombre} al borrador`);
            add.addEventListener('click', () => {
                const existing = selected.get(p.id);
                const qty = existing ? existing.cantidad + 1 : 1;
                if (p.stock !== null && qty > Math.floor(p.stock)) {
                    status.textContent = `La cantidad supera la disponibilidad consultada de ${p.nombre}.`;
                    return;
                }
                invalidate();
                selected.set(p.id, { ...p, cantidad: qty });
                status.textContent = `${p.nombre} añadido al borrador.`;
                renderCart();
            });
            bottom.append(figures, add);
            panel.append(heading, bottom);
            results.appendChild(panel);
        });
    };

    const renderCart = () => {
        cart.replaceChildren();
        const items = [...selected.values()];
        if (!items.length) cart.appendChild(node('p', 'kac-empty', 'No hay productos añadidos.'));
        for (const item of items) {
            const row = node('div', 'kac-cart-line');
            const detail = node('div');
            detail.appendChild(node('strong', '', item.nombre));
            detail.appendChild(node('small', '', `${item.codigo || `#${item.id}`} · ${item.precio === null ? 'Precio pendiente' : money.format(item.precio)}`));
            const tools = node('div', 'kac-cart-edit');
            const quantity = node('input');
            quantity.type = 'number'; quantity.min = '1'; quantity.max = '9999'; quantity.step = '1';
            quantity.value = String(item.cantidad);
            quantity.setAttribute('aria-label', `Cantidad de ${item.nombre}`);
            quantity.addEventListener('change', () => {
                const q = Number(quantity.value);
                if (!Number.isSafeInteger(q) || q < 1 || q > 9999 || (item.stock !== null && q > Math.floor(item.stock))) {
                    quantity.value = String(item.cantidad);
                    status.textContent = `Cantidad inválida o superior al stock consultado de ${item.nombre}.`;
                    return;
                }
                invalidate();
                item.cantidad = q;
                renderCart();
            });
            const remove = node('button', '', 'Quitar');
            remove.type = 'button';
            remove.setAttribute('aria-label', `Quitar ${item.nombre}`);
            remove.addEventListener('click', () => { invalidate(); selected.delete(item.id); renderCart(); });
            tools.append(quantity, remove);
            row.append(detail, tools);
            cart.appendChild(row);
        }
        const anyWithoutPrice = items.some(p => p.precio === null);
        const sum = items.reduce((acc, p) => acc + ((p.precio ?? 0) * p.cantidad), 0);
        total.textContent = items.length === 0 || anyWithoutPrice ? '—' : money.format(sum);
        clear.disabled = items.length === 0;
        validate.disabled = items.length === 0 || validating;
        copy.disabled = verified === null;
    };

    // Fase 5: propuesta revalidada SIEMPRE en el servidor.
    // El navegador solo envía IDs y cantidades; el backend ignora precios locales.
    validate?.addEventListener('click', async () => {
        if (validating || selected.size === 0) return;
        invalidate();
        validating = true;
        validate.disabled = true;
        review.textContent = 'Verificando productos, precios y stock de tu empresa en SQL Server...';
        status.textContent = '';
        const detalle = [...selected.values()].map(p => ({ idProducto: p.id, cantidad: p.cantidad }));
        const cliente = el('kacCliente').value.trim();
        const notas = el('kacNotas').value.trim();
        const token = form?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const validateUrl = new URL(form.action, location.href);
        validateUrl.searchParams.set('handler', 'ValidarBorrador');
        try {
            const res = await fetch(validateUrl.toString(), {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8', 'Accept': 'application/json' },
                body: new URLSearchParams({ detalle: JSON.stringify(detalle), cliente, notas, __RequestVerificationToken: token })
            });
            const json = await res.json();
            if (!res.ok || json.ok !== true || json.registrado !== false)
                throw new Error(json.mensaje || `No se pudo validar el borrador (HTTP ${res.status}).`);
            if (!Array.isArray(json.lineas) || json.lineas.length !== detalle.length || typeof json.textoBorrador !== 'string')
                throw new Error('El servidor devolvió una validación incompleta.');
            verified = json;
            copy.disabled = false;
            total.textContent = json.importeReferencial == null ? 'Pendiente' : money.format(Number(json.importeReferencial));
            const warns = Array.isArray(json.advertencias) ? json.advertencias : [];
            review.className = warns.length ? 'kac-review is-warning' : 'kac-review is-valid';
            review.textContent = warns.length
                ? `Borrador revalidado con ${warns.length} advertencia(s):\n• ${warns.join('\n• ')}\nNo registrado. Revisar antes de cualquier operación comercial.`
                : `Borrador revalidado: ${json.lineas.length} línea(s). Importes recalculados en SQL. Pendiente de aprobación y registro. No se reservó stock.`;
            status.textContent = 'Borrador validado en servidor. Puedes copiarlo para revisión humana.';
        } catch (error) {
            verified = null;
            copy.disabled = true;
            review.className = 'kac-review is-error';
            review.textContent = error?.message || 'No fue posible validar. Intenta nuevamente.';
            status.textContent = 'Sin validación del servidor no se permite copiar un borrador verificado.';
        } finally {
            validating = false;
            validate.disabled = selected.size === 0;
        }
    });

    ['kacCliente', 'kacNotas'].forEach(id => {
        el(id)?.addEventListener('input', () => { if (verified) invalidate(); });
    });

    form?.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (busy) return;
        const query = el('kacConsulta').value.trim();
        if (query.length < 2 || query.length > 120) {
            bubble('Indica al menos dos caracteres del producto o su código.');
            return;
        }
        busy = true;
        button.disabled = true;
        bubble(query, 'user');
        bubble('Consultando el catálogo de KerniX...');
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        try {
            const payload = new URLSearchParams({ consulta: query, __RequestVerificationToken: token });
            const response = await fetch(form.action, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8', 'Accept': 'application/json' },
                body: payload
            });
            const data = await response.json();
            if (!response.ok || !data.ok) throw new Error(data.mensaje ?? 'Falló la consulta del catálogo.');
            const raw = Array.isArray(data.items) ? data.items : [];
            found = raw.map(toItem);
            renderResults();
            bubble(found.length ? `Encontré ${found.length} producto(s) en el catálogo de tu empresa. Revisa precios y existencias antes de añadirlos a una propuesta.` : 'No encontré productos activos con esos términos. Prueba otro nombre o código.');
            if (found.length) results.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } catch (error) {
            found = [];
            renderResults();
            bubble(error?.message || 'No fue posible consultar el catálogo.');
        } finally {
            busy = false;
            button.disabled = false;
        }
    });

    clear?.addEventListener('click', () => { invalidate(); selected.clear(); status.textContent = ''; renderCart(); });
    copy?.addEventListener('click', async () => {
        try {
            if (!verified || !verified.textoBorrador) return;
            await navigator.clipboard.writeText(verified.textoBorrador);
            status.textContent = 'Borrador verificado copiado. Pendiente de aprobación. No se registró ninguna transacción.';
        } catch {
            status.textContent = 'El navegador bloqueó la copia. Revisa permisos del portapapeles.';
        }
    });
    // Un único resultado visual para búsquedas manuales y consultas del chat IA.
    // El origen son productos devueltos por KerniX Web (empresa validada en backend).
    window.KerniXCatalogoUI = Object.freeze({
        cargarDesdeChat: (items, sugerencia = null) => {
            found = Array.isArray(items) ? items.map(toItem).filter(p => Number.isSafeInteger(p.id) && p.id > 0) : [];
            renderResults();
            // Solo se prellena un BORRADOR si el mensaje pedía cotizar una cantidad,
            // y el servidor encontró UN ÚNICO producto inequívoco con stock y precio.
            // Aun así requiere hacer clic en «Validar borrador en SQL».
            const candidate = found.length === 1 ? found[0] : null;
            const id = Number(sugerencia?.idProducto ?? 0);
            const qty = Number(sugerencia?.cantidad ?? 0);
            if (candidate && candidate.id === id && Number.isSafeInteger(qty) && qty >= 1 && qty <= 9999 &&
                candidate.precio !== null && candidate.stock !== null && candidate.stock >= qty) {
                invalidate();
                selected.set(candidate.id, { ...candidate, cantidad: qty });
                renderCart();
                status.textContent = `${candidate.nombre} x ${qty} preseleccionado por solicitud explícita. REVISA y VALIDA antes de copiar.`;
            } else {
                status.textContent = found.length
                    ? `KerniX IA consultó ${found.length} producto(s) reales. Selecciona el artículo y la cantidad para generar un borrador.`
                    : 'No se encontraron productos para esa consulta.';
            }
        }
    });
    renderCart();
})();
