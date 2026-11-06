// Chat flotante del asistente virtual. JavaScript puro (no depende de jQuery ni de Bootstrap).
//   - Manda la conversación a POST /Asistente/Preguntar con el token antifalsificación en la cabecera.
//   - La conversación se guarda en sessionStorage para que sobreviva al cambiar de pantalla; se borra al cerrar la pestaña.
(function () {
    const raiz = document.getElementById('asistente');
    if (!raiz) return;

    const boton = document.getElementById('asistenteAbrir');
    const panel = document.getElementById('asistentePanel');
    const log = document.getElementById('asistenteLog');
    const form = document.getElementById('asistenteForm');
    const texto = document.getElementById('asistenteTexto');
    const enviar = document.getElementById('asistenteEnviar');
    const sugerencias = document.getElementById('asistenteSugerencias');
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const CLAVE = 'asistente.conversacion.v1';
    const MAX_MENSAJES = 12;
    const BIENVENIDA = 'Hola, soy **Sonri**, el asistente de Sonrisa Dental. Puedo contarte sobre horarios, tratamientos, ' +
        'precios y obras sociales, ayudarte a elegir qué especialidad consultar o explicarte cómo usar el sistema.';

    let mensajes = cargar();
    let esperando = false;

    // ----- Persistencia (puede no estar disponible: se ignora el error) -----
    function cargar() {
        try {
            const guardado = JSON.parse(sessionStorage.getItem(CLAVE) || 'null');
            // Si en esta pestaña inició sesión otra persona, no se muestra la conversación anterior.
            if (guardado && guardado.usuario === raiz.dataset.usuario && Array.isArray(guardado.mensajes)) return guardado.mensajes;
        } catch (e) { /* sin almacenamiento */ }
        return [];
    }

    function guardar() {
        try { sessionStorage.setItem(CLAVE, JSON.stringify({ usuario: raiz.dataset.usuario, mensajes })); } catch (e) { /* sin almacenamiento */ }
    }

    // ----- Dibujo -----
    // El texto del asistente se escapa primero y recién después se le agregan etiquetas propias (negritas y listas),
    // así nada de lo que llegue de la API puede inyectar HTML.
    function escapar(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function enLinea(s) {
        return s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    }

    function formatear(t) {
        let html = '';
        let lista = false;
        for (const linea of escapar(t).split(/\r?\n/)) {
            const item = linea.match(/^\s*[-*•]\s+(.*)$/);
            if (item) {
                if (!lista) { html += '<ul>'; lista = true; }
                html += '<li>' + enLinea(item[1]) + '</li>';
                continue;
            }
            if (lista) { html += '</ul>'; lista = false; }
            if (linea.trim() !== '') html += '<p>' + enLinea(linea) + '</p>';
        }
        return html + (lista ? '</ul>' : '');
    }

    function burbuja(rol, contenido, error) {
        const div = document.createElement('div');
        div.className = 'asistente-msg ' + (rol === 'user' ? 'propio' : 'respuesta') + (error ? ' error' : '');
        if (rol === 'user') div.textContent = contenido;
        else div.innerHTML = formatear(contenido);
        log.appendChild(div);
        log.scrollTop = log.scrollHeight;
        return div;
    }

    function dibujarTodo() {
        log.innerHTML = '';
        burbuja('model', BIENVENIDA);
        mensajes.forEach(m => burbuja(m.rol, m.texto));
        sugerencias.hidden = mensajes.length > 0;
    }

    // ----- Abrir y cerrar -----
    function abrir() {
        panel.hidden = false;
        boton.setAttribute('aria-expanded', 'true');
        raiz.classList.add('abierto');
        texto.focus();
        log.scrollTop = log.scrollHeight;
    }

    function cerrar() {
        panel.hidden = true;
        boton.setAttribute('aria-expanded', 'false');
        raiz.classList.remove('abierto');
        boton.focus();
    }

    boton.addEventListener('click', () => (panel.hidden ? abrir() : cerrar()));
    document.getElementById('asistenteCerrar').addEventListener('click', cerrar);
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && !panel.hidden) cerrar(); });

    document.getElementById('asistenteBorrar').addEventListener('click', () => {
        if (esperando) return;
        mensajes = [];
        guardar();
        dibujarTodo();
        texto.focus();
    });

    sugerencias.addEventListener('click', e => {
        const chip = e.target.closest('.asistente-chip');
        if (chip) preguntar(chip.textContent.trim());
    });

    // ----- Envío -----
    function ajustarAltura() {
        texto.style.height = 'auto';
        // +2: el alto incluye el borde (box-sizing: border-box) y sin eso aparecían flechas de scroll de más.
        texto.style.height = Math.min(texto.scrollHeight + 2, 110) + 'px';
        texto.style.overflowY = texto.scrollHeight + 2 > 110 ? 'auto' : 'hidden';
    }

    texto.addEventListener('input', ajustarAltura);
    texto.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); form.requestSubmit(); }
    });
    form.addEventListener('submit', e => {
        e.preventDefault();
        preguntar(texto.value);
    });

    function fijarEspera(valor) {
        esperando = valor;
        enviar.disabled = valor;
        texto.disabled = valor;
        raiz.classList.toggle('esperando', valor);
    }

    async function preguntar(pregunta) {
        pregunta = (pregunta || '').trim();
        if (!pregunta || esperando) return;

        mensajes.push({ rol: 'user', texto: pregunta });
        mensajes = mensajes.slice(-MAX_MENSAJES);
        guardar();
        sugerencias.hidden = true;
        const burbujaPropia = burbuja('user', pregunta);
        texto.value = '';
        ajustarAltura();

        fijarEspera(true);
        const escribiendo = burbuja('model', '…');
        escribiendo.classList.add('escribiendo');
        escribiendo.setAttribute('aria-label', 'El asistente está escribiendo');

        let respuesta = 'No pude obtener una respuesta. Probá de nuevo en un momento.';
        let ok = false;
        try {
            const r = await fetch(raiz.dataset.url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
                body: JSON.stringify({ mensajes: mensajes.map(m => ({ rol: m.rol, texto: m.texto })) })
            });
            const tipo = r.headers.get('content-type') || '';
            if (tipo.includes('application/json')) {
                const datos = await r.json();
                respuesta = datos.texto || respuesta;
                ok = !!datos.ok;
            } else if (r.status === 400 || r.redirected || r.status === 401) {
                respuesta = 'Tu sesión venció. Recargá la página e ingresá de nuevo.';
            }
        } catch (err) {
            respuesta = 'No hay conexión con el servidor. Revisá tu internet y probá de nuevo.';
        }

        escribiendo.remove();
        burbuja('model', respuesta, !ok);
        if (ok) {
            mensajes.push({ rol: 'model', texto: respuesta });
            mensajes = mensajes.slice(-MAX_MENSAJES);
        } else {
            // Si falló, la pregunta sale del historial y vuelve al cuadro de texto para reintentar sin duplicarla.
            mensajes.pop();
            burbujaPropia.remove();
            texto.value = pregunta;
        }
        guardar();
        fijarEspera(false);
        ajustarAltura();
        texto.focus();
    }

    dibujarTodo();
})();
