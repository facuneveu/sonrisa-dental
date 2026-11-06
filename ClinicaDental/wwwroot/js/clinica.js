// Comportamiento propio de los formularios clínicos (mensajes y archivos). Se engancha al modal de Crud con
// `alAbrir: Clinica.alAbrir`, que se ejecuta cada vez que se dibuja un formulario dentro del modal.
window.Clinica = (function () {

    // ----- Mensajes: plantillas según el motivo -----
    // Las claves son los valores del enum TipoMensaje (0 Demora, 1 Reprogramación, 2 Cancelación, 3 Indicaciones, 4 Otro).
    const plantillas = {
        '0': (n, t) => ({
            asunto: 'Demora en tu turno',
            texto: `Hola${n}, te escribo porque estoy con una demora y ${t ? `tu turno (${t})` : 'tu turno'} va a comenzar unos minutos más tarde. Disculpá las molestias y gracias por la paciencia.`
        }),
        '1': (n, t) => ({
            asunto: 'Necesito reprogramar tu turno',
            texto: `Hola${n}, por un inconveniente necesito reprogramar ${t ? `tu turno (${t})` : 'tu turno'}. Podés cancelarlo desde "Mis turnos" y sacar uno nuevo en el horario que prefieras. Disculpá las molestias.`
        }),
        '2': (n, t) => ({
            asunto: 'Tu turno fue cancelado por un imprevisto',
            texto: `Hola${n}, lamentablemente tuve que cancelar ${t ? `tu turno (${t})` : 'tu turno'} por un imprevisto. Podés sacar uno nuevo desde "Mis turnos" cuando quieras. Pedimos disculpas por el inconveniente.`
        }),
        '3': (n, t) => ({
            asunto: 'Indicaciones para tu turno',
            texto: `Hola${n}, te dejo algunas indicaciones para ${t ? `tu turno (${t})` : 'tu próximo turno'}: `
        }),
        '4': () => ({ asunto: '', texto: '' })
    };

    function iniciarFormularioMensaje(raiz) {
        const form = raiz.querySelector('#formMensaje');
        if (!form) return;

        const tipo = form.querySelector('#Tipo');
        const asunto = form.querySelector('#Asunto');
        const texto = form.querySelector('#Texto');
        const paciente = form.querySelector('select#PacienteId');   // no existe si se abrió desde un turno
        const turno = form.querySelector('select#TurnoId');
        const contador = form.querySelector('#contadorTexto');

        // Si la persona ya escribió algo (o el formulario volvió con errores), la plantilla no lo pisa.
        let editadoAsunto = asunto.value.trim() !== '';
        let editadoTexto = texto.value.trim() !== '';

        const actualizarContador = () => { contador.textContent = texto.value.length; };
        asunto.addEventListener('input', () => { editadoAsunto = true; });
        texto.addEventListener('input', () => { editadoTexto = true; actualizarContador(); });

        function nombrePila() {
            if (form.dataset.nombre) return ' ' + form.dataset.nombre;
            if (paciente && paciente.selectedIndex > 0 && paciente.value !== '0') {
                // Las opciones son "Apellido, Nombre"
                const partes = paciente.selectedOptions[0].text.split(',');
                return partes.length > 1 ? ' ' + partes[1].trim().split(' ')[0] : '';
            }
            return '';
        }

        function textoTurno() {
            if (form.dataset.turno) return form.dataset.turno;
            return turno && turno.value ? turno.selectedOptions[0].text : '';
        }

        function aplicarPlantilla() {
            const plantilla = plantillas[tipo.value]?.(nombrePila(), textoTurno());
            if (!plantilla) return;
            if (!editadoAsunto) asunto.value = plantilla.asunto;
            if (!editadoTexto) texto.value = plantilla.texto;
            actualizarContador();
        }

        async function cargarTurnos() {
            if (!turno) return;
            turno.innerHTML = '';
            turno.add(new Option('Sin turno específico', ''));
            if (!paciente.value || paciente.value === '0') return;

            const lista = await fetch(`${form.dataset.urlTurnos}?pacienteId=${paciente.value}`).then(r => r.json());
            lista.forEach(t => turno.add(new Option(t.texto, t.id)));
        }

        tipo.addEventListener('change', aplicarPlantilla);
        turno?.addEventListener('change', aplicarPlantilla);
        paciente?.addEventListener('change', async () => { await cargarTurnos(); aplicarPlantilla(); });

        cargarTurnos().then(aplicarPlantilla);
    }

    // ----- Archivos: tamaño máximo y vista previa antes de subir -----
    function iniciarFormularioAdjunto(raiz) {
        const input = raiz.querySelector('#Archivo');
        if (!input || !input.dataset.maxMb) return;

        const maxMb = Number(input.dataset.maxMb);
        const vista = raiz.querySelector('#vistaPrevia');
        const img = vista?.querySelector('img');

        input.addEventListener('change', () => {
            vista?.classList.add('d-none');
            const archivo = input.files[0];
            if (!archivo) return;

            if (archivo.size > maxMb * 1024 * 1024) {
                Swal.fire('Archivo demasiado grande', `El tamaño máximo es ${maxMb} MB.`, 'warning');
                input.value = '';
                return;
            }
            if (archivo.type.startsWith('image/') && img) {
                img.src = URL.createObjectURL(archivo);
                vista.classList.remove('d-none');
            }
        });
    }

    function alAbrir(raiz) {
        iniciarFormularioMensaje(raiz);
        iniciarFormularioAdjunto(raiz);
    }

    return { alAbrir };
})();
