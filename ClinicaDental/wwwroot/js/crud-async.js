window.Crud = (function () {
    let cfg, modal, contenido, tabla;

    const toast = Swal.mixin({
        toast: true, position: 'top-end', showConfirmButton: false,
        timer: 2800, timerProgressBar: true
    });

    function init(config) {
        cfg = config;
        tabla = document.getElementById(cfg.tablaId);

        const el = document.getElementById(cfg.modalId);
        if (el) {
            modal = new bootstrap.Modal(el);
            contenido = el.querySelector('.modal-content');
        }
    }

    function urlListado() {
        return typeof cfg.listarUrl === 'function' ? cfg.listarUrl() : cfg.listarUrl;
    }

    function alDibujarFormulario() {
        const form = contenido.querySelector('form');
        if (form && $.validator && $.validator.unobtrusive) {
            $(form).removeData('validator').removeData('unobtrusiveValidation');
            $.validator.unobtrusive.parse(form);
        }
        if (typeof cfg.alAbrir === 'function') cfg.alAbrir(contenido);
    }

    function refrescar() {
        return fetch(urlListado())
            .then(r => r.text())
            .then(html => { tabla.innerHTML = html; });
    }

    function abrir(url) {
        if (!modal) return;
        fetch(url)
            .then(r => {
                if (r.status === 401 || r.status === 403) { location.reload(); throw new Error('sin permiso'); }
                return r.text();
            })
            .then(html => {
                contenido.innerHTML = html;
                alDibujarFormulario();
                modal.show();
            })
            .catch(() => {});
    }

    function guardar(event) {
        event.preventDefault();
        const form = event.target;
        if (!$(form).valid()) return;

        const boton = form.querySelector('button[type="submit"]');
        if (boton) boton.disabled = true;

        fetch(form.action, { method: 'POST', body: new FormData(form) })
            .then(res => {
                if ((res.headers.get('content-type') || '').includes('application/json')) {
                    return res.json().then(data => {
                        if (data.success) {
                            modal.hide();
                            refrescar();
                            toast.fire({ icon: 'success', title: data.mensaje });
                        } else {
                            if (boton) boton.disabled = false;
                            Swal.fire('No se pudo guardar', data.mensaje, 'error');
                        }
                    });
                }
                return res.text().then(html => { contenido.innerHTML = html; alDibujarFormulario(); });
            })
            .catch(() => {
                if (boton) boton.disabled = false;
                Swal.fire('Error de conexión', 'No pudimos comunicarnos con el servidor. Intentá nuevamente.', 'error');
            });
    }

    function eliminar(id, nombre) {
        Swal.fire({
            title: `¿Eliminar ${cfg.entidad}?`,
            html: `Se eliminará <b></b>. Esta acción no se puede deshacer.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#b53a3a',
            cancelButtonColor: '#51696f',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            didOpen: () => { Swal.getHtmlContainer().querySelector('b').textContent = nombre; }
        }).then(result => {
            if (!result.isConfirmed) return;
            enviarAccion(cfg.eliminarUrl, id, 'No se pudo eliminar');
        });
    }

    function enviarAccion(url, id, tituloError, extra) {
        const fd = new FormData();
        fd.append('id', id);
        if (extra) Object.keys(extra).forEach(k => fd.append(k, extra[k]));
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) fd.append('__RequestVerificationToken', token);

        return fetch(url, { method: 'POST', body: fd })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    refrescar();
                    toast.fire({ icon: 'success', title: data.mensaje });
                } else {
                    Swal.fire(tituloError || 'No se pudo completar', data.mensaje, 'error');
                }
                return data;
            })
            .catch(() => Swal.fire('Error de conexión', 'No pudimos comunicarnos con el servidor.', 'error'));
    }

    return { init, abrir, guardar, eliminar, refrescar, enviarAccion, toast };
})();

window.conRetraso = function (fn, ms) {
    let t;
    return function () { clearTimeout(t); t = setTimeout(fn, ms || 300); };
};
