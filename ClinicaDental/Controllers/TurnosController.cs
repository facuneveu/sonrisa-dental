using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    // Un mismo controlador para los tres roles, con alcance distinto:
    //   Administrador -> ve y gestiona todos los turnos
    //   Odontólogo    -> ve su agenda y cambia el estado de SUS turnos
    //   Paciente      -> ve sus turnos, reserva y cancela los suyos
    // El alcance se aplica siempre en el servidor (nunca confiando en lo que mande el navegador).
    [Authorize]
    public class TurnosController : Controller
    {
        private const string AdminOPaciente = Roles.Administrador + "," + Roles.Paciente;

        private readonly ApplicationDbContext _context;
        private readonly DisponibilidadService _disponibilidad;
        private readonly PerfilService _perfil;

        public TurnosController(ApplicationDbContext context, DisponibilidadService disponibilidad, PerfilService perfil)
        {
            _context = context;
            _disponibilidad = disponibilidad;
            _perfil = perfil;
        }

        private bool EsAdmin => User.IsInRole(Roles.Administrador);

        // ------------------------------------------------------------------ Listado

        private async Task<List<Turno>> ListarAsync(DateOnly? desde, DateOnly? hasta, EstadoTurno? estado, int? odontologoId, int? pacienteId, string? q)
        {
            var consulta = _context.Turnos.AsNoTracking()
                .Include(t => t.Paciente!).ThenInclude(p => p.Usuario)
                .Include(t => t.Paciente!).ThenInclude(p => p.ObraSocial)
                .Include(t => t.Odontologo!).ThenInclude(o => o.Usuario)
                .Include(t => t.Tratamiento)
                .AsQueryable();

            // Alcance según el rol
            if (User.IsInRole(Roles.Odontologo) && !EsAdmin)
            {
                var id = await _perfil.ObtenerOdontologoIdAsync();
                consulta = consulta.Where(t => t.OdontologoId == id);
            }
            else if (User.IsInRole(Roles.Paciente) && !EsAdmin)
            {
                var id = await _perfil.ObtenerPacienteIdAsync();
                consulta = consulta.Where(t => t.PacienteId == id);
            }
            else if (EsAdmin)
            {
                // Los filtros por persona solo los usa la administración (los demás roles ya están acotados a lo suyo).
                if (odontologoId is > 0) consulta = consulta.Where(t => t.OdontologoId == odontologoId);
                if (pacienteId is > 0) consulta = consulta.Where(t => t.PacienteId == pacienteId);
            }

            if (desde is { } d) consulta = consulta.Where(t => t.FechaHora >= d.ToDateTime(TimeOnly.MinValue));
            if (hasta is { } h) consulta = consulta.Where(t => t.FechaHora < h.AddDays(1).ToDateTime(TimeOnly.MinValue));
            if (estado is { } e) consulta = consulta.Where(t => t.Estado == e);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];   // un texto enorme no tiene sentido y rompe la consulta
                consulta = consulta.Where(t => t.Paciente!.Usuario!.Nombre.Contains(texto) || t.Paciente.Usuario.Apellido.Contains(texto)
                                               || t.Odontologo!.Usuario!.Apellido.Contains(texto) || t.Tratamiento!.Nombre.Contains(texto)
                                               || (t.Motivo != null && t.Motivo.Contains(texto)));
            }

            // Mirando hacia adelante, el más próximo primero; mirando el historial, el más reciente primero.
            return await (desde.HasValue ? consulta.OrderBy(t => t.FechaHora) : consulta.OrderByDescending(t => t.FechaHora))
                .Take(300).ToListAsync();
        }

        // Los parámetros permiten llegar desde otras pantallas con la lista ya filtrada
        // (por ejemplo, los números del inicio o "ver turnos" de un paciente) o con el formulario de reserva abierto.
        public async Task<IActionResult> Index(string? periodo, EstadoTurno? estado, int? odontologoId, int? pacienteId,
            bool nuevo = false, int tratamientoId = 0, int profesionalId = 0)
        {
            var hoy = Reloj.Hoy;
            var (desde, hasta) = Periodo(periodo, hoy);

            ViewBag.Hoy = hoy.ToString("yyyy-MM-dd");
            ViewBag.DesdeInicial = desde?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.HastaInicial = hasta?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.EstadoInicial = estado?.ToString() ?? "";
            int? odontologoFiltro = EsAdmin ? odontologoId : null;
            int? pacienteFiltro = EsAdmin ? pacienteId : null;
            ViewBag.OdontologoInicial = odontologoFiltro;

            if (EsAdmin)
            {
                ViewBag.Odontologos = await _context.Odontologos.AsNoTracking()
                    .OrderBy(o => o.Usuario!.Apellido).ThenBy(o => o.Usuario!.Nombre)
                    .Select(o => new SelectListItem(o.Usuario!.Apellido + ", " + o.Usuario.Nombre, o.Id.ToString())).ToListAsync();

                if (pacienteFiltro is > 0)
                {
                    ViewBag.PacienteInicial = pacienteFiltro;
                    ViewBag.PacienteNombre = await _context.Pacientes.AsNoTracking().Where(p => p.Id == pacienteFiltro)
                        .Select(p => p.Usuario!.Apellido + ", " + p.Usuario.Nombre).FirstOrDefaultAsync();
                }
            }

            if (nuevo && (EsAdmin || User.IsInRole(Roles.Paciente)))
                ViewBag.AbrirFormularioUrl = Url.Action(nameof(ObtenerFormulario), "Turnos", new { id = 0, tratamientoId, profesionalId });

            return View(await ListarAsync(desde, hasta, estado, odontologoFiltro, pacienteFiltro, null));
        }

        // "proximos" (por defecto) | "hoy" | "semana" | "historial" | "todos"
        private static (DateOnly? Desde, DateOnly? Hasta) Periodo(string? periodo, DateOnly hoy) => periodo switch
        {
            "hoy" => (hoy, hoy),
            "semana" => (hoy, hoy.AddDays(6)),
            "historial" => (null, hoy.AddDays(-1)),
            "todos" => (null, null),
            _ => (hoy, null)
        };

        [HttpGet]
        public async Task<IActionResult> ListarPartial(DateOnly? desde, DateOnly? hasta, EstadoTurno? estado, int? odontologoId, int? pacienteId, string? q) =>
            PartialView("_TablaTurnosPartial", await ListarAsync(desde, hasta, estado, odontologoId, pacienteId, q));

        // ------------------------------------------------------------------ Alta / edición

        [HttpGet]
        [Authorize(Roles = AdminOPaciente)]
        public async Task<IActionResult> ObtenerFormulario(int id = 0, int tratamientoId = 0, int profesionalId = 0)
        {
            if (id == 0)
            {
                await CargarListasAsync();
                // Precarga cuando se llega desde el catálogo o desde un profesional. Guardar() revalida todo igual.
                return PartialView("_FormularioTurnoPartial", new TurnoFormViewModel
                {
                    TratamientoId = Math.Max(tratamientoId, 0),
                    OdontologoId = tratamientoId > 0 ? Math.Max(profesionalId, 0) : 0
                });
            }

            // Editar un turno existente es solo de la administración.
            if (!EsAdmin) return Forbid();

            var turno = await _context.Turnos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (turno == null) return NotFound();

            await CargarListasAsync(turno.TratamientoId);
            return PartialView("_FormularioTurnoPartial", new TurnoFormViewModel
            {
                Id = turno.Id,
                PacienteId = turno.PacienteId,
                TratamientoId = turno.TratamientoId,
                OdontologoId = turno.OdontologoId,
                Fecha = DateOnly.FromDateTime(turno.FechaHora),
                Hora = TimeOnly.FromDateTime(turno.FechaHora),
                Motivo = turno.Motivo,
                Estado = turno.Estado,
                Observaciones = turno.Observaciones
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AdminOPaciente)]
        public async Task<IActionResult> Guardar(TurnoFormViewModel vm)
        {
            Turno? turno = null;
            if (vm.Id != 0)
            {
                if (!EsAdmin)
                    return Json(new { success = false, mensaje = "Solo la administración puede modificar un turno. Podés cancelarlo y sacar otro." });

                turno = await _context.Turnos.FirstOrDefaultAsync(t => t.Id == vm.Id);
                if (turno == null) return Json(new { success = false, mensaje = "El turno no existe." });
            }

            // ---- Paciente: la administración lo elige; un paciente solo puede reservar para sí mismo.
            int pacienteId;
            if (EsAdmin)
            {
                pacienteId = vm.PacienteId;
                if (!await _context.Pacientes.AnyAsync(p => p.Id == pacienteId && (p.Usuario!.Activo || (turno != null && turno.PacienteId == pacienteId))))
                    ModelState.AddModelError(nameof(vm.PacienteId), "Seleccioná un paciente.");
            }
            else
            {
                pacienteId = await _perfil.ObtenerPacienteIdAsync() ?? 0;
                if (pacienteId == 0)
                    return Json(new { success = false, mensaje = "Tu cuenta no tiene un perfil de paciente. Comunicate con la clínica." });
            }

            // ---- Tratamiento y odontólogo
            var tratamiento = await _context.Tratamientos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == vm.TratamientoId);
            if (tratamiento == null)
                ModelState.AddModelError(nameof(vm.TratamientoId), "El tratamiento elegido no existe.");
            else if (!tratamiento.Activo && (turno == null || turno.TratamientoId != tratamiento.Id))
                ModelState.AddModelError(nameof(vm.TratamientoId), "Ese tratamiento no está disponible por el momento.");

            var odontologo = await _context.Odontologos.AsNoTracking().Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.Id == vm.OdontologoId);
            if (odontologo == null)
                ModelState.AddModelError(nameof(vm.OdontologoId), "El odontólogo elegido no existe.");
            else
            {
                if (!odontologo.Usuario!.Activo && (turno == null || turno.OdontologoId != odontologo.Id))
                    ModelState.AddModelError(nameof(vm.OdontologoId), "Ese odontólogo no está atendiendo por el momento.");
                if (tratamiento != null && odontologo.EspecialidadId != tratamiento.EspecialidadId)
                    ModelState.AddModelError(nameof(vm.OdontologoId), "Ese odontólogo no realiza el tratamiento elegido.");
            }

            // ---- Fecha, hora y disponibilidad (solo si algo de eso cambió)
            if (ModelState.IsValid && tratamiento != null && odontologo != null)
            {
                var inicio = vm.Fecha!.Value.ToDateTime(vm.Hora!.Value);
                var duracion = tratamiento.DuracionMinutos;

                var cambioAgenda = turno == null || turno.FechaHora != inicio
                                   || turno.OdontologoId != odontologo.Id || turno.DuracionMinutos != duracion;
                if (cambioAgenda)
                {
                    if (inicio <= Reloj.Ahora)
                        ModelState.AddModelError(nameof(vm.Hora), "El horario elegido ya pasó.");
                    else if (vm.Fecha!.Value > Reloj.Hoy.AddYears(1))
                        ModelState.AddModelError(nameof(vm.Fecha), "Solo se pueden reservar turnos con hasta un año de anticipación.");
                    else
                    {
                        var motivo = await _disponibilidad.ValidarAsync(odontologo.Id, inicio, duracion, turno?.Id);
                        if (motivo != null)
                            ModelState.AddModelError(nameof(vm.Hora), motivo);
                        else if (await _disponibilidad.PacienteOcupadoAsync(pacienteId, inicio, duracion, turno?.Id))
                            ModelState.AddModelError(nameof(vm.Hora), "El paciente ya tiene otro turno en ese horario.");
                    }
                }

                if (ModelState.IsValid)
                {
                    if (turno == null)
                    {
                        turno = new Turno { Estado = EstadoTurno.Pendiente };
                        _context.Turnos.Add(turno);
                    }

                    turno.PacienteId = pacienteId;
                    turno.OdontologoId = odontologo.Id;
                    turno.TratamientoId = tratamiento.Id;
                    turno.FechaHora = inicio;
                    turno.DuracionMinutos = duracion;
                    turno.Motivo = string.IsNullOrWhiteSpace(vm.Motivo) ? null : vm.Motivo.Trim();

                    if (EsAdmin)
                    {
                        turno.Estado = vm.Estado;
                        turno.Observaciones = string.IsNullOrWhiteSpace(vm.Observaciones) ? null : vm.Observaciones.Trim();
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                        return Json(new
                        {
                            success = true,
                            mensaje = vm.Id == 0 ? "Turno reservado correctamente." : "Turno actualizado correctamente."
                        });
                    }
                    catch (DbUpdateException)
                    {
                        // El índice único de la base frenó una doble reserva simultánea.
                        ModelState.AddModelError(nameof(vm.Hora), "Ese horario acaba de ser reservado por otra persona. Elegí otro.");
                    }
                }
            }

            await CargarListasAsync(turno?.TratamientoId);
            return PartialView("_FormularioTurnoPartial", vm);
        }

        // ------------------------------------------------------------------ Cambios de estado

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoTurno estado)
        {
            var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.Id == id);
            if (turno == null)
                return Json(new { success = false, mensaje = "El turno no existe." });

            var propio = EsAdmin
                || (User.IsInRole(Roles.Odontologo) && turno.OdontologoId == await _perfil.ObtenerOdontologoIdAsync())
                || (User.IsInRole(Roles.Paciente) && turno.PacienteId == await _perfil.ObtenerPacienteIdAsync());
            if (!propio)
                return Json(new { success = false, mensaje = "No tenés permiso para modificar este turno." });

            if (!TurnoReglas.PuedeCambiar(turno.Estado, estado))
                return Json(new { success = false, mensaje = $"Un turno {turno.Estado.ToString().ToLowerInvariant()} no puede pasar a {estado.ToString().ToLowerInvariant()}." });

            if (User.IsInRole(Roles.Paciente) && !EsAdmin)
            {
                if (estado != EstadoTurno.Cancelado)
                    return Json(new { success = false, mensaje = "Los pacientes solo pueden cancelar sus turnos." });
                if (!TurnoReglas.PacientePuedeCancelar(turno, Reloj.Ahora))
                    return Json(new { success = false, mensaje = "No podés cancelar un turno que ya pasó." });
            }

            turno.Estado = estado;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = estado == EstadoTurno.Cancelado ? "Turno cancelado." : $"Turno marcado como {estado.ToString().ToLowerInvariant()}."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null)
                return Json(new { success = false, mensaje = "El turno no existe." });

            if (await _context.HistoriasClinicas.AnyAsync(h => h.TurnoId == id))
                return Json(new { success = false, mensaje = "El turno tiene una historia clínica asociada y no puede eliminarse." });

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Turno eliminado correctamente." });
        }

        // ------------------------------------------------------------------ Datos para el formulario (AJAX)

        // Al elegir un tratamiento: duración, precio y los odontólogos que lo realizan.
        [HttpGet]
        [Authorize(Roles = AdminOPaciente)]
        public async Task<IActionResult> OdontologosPorTratamiento(int tratamientoId)
        {
            var tratamiento = await _context.Tratamientos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tratamientoId);
            if (tratamiento == null) return NotFound();

            var odontologos = await _context.Odontologos.AsNoTracking()
                .Where(o => o.EspecialidadId == tratamiento.EspecialidadId && o.Usuario!.Activo && o.Horarios.Any())
                .OrderBy(o => o.Usuario!.Apellido).ThenBy(o => o.Usuario!.Nombre)
                .Select(o => new { id = o.Id, nombre = o.Usuario!.Nombre + " " + o.Usuario.Apellido })
                .ToListAsync();

            return Json(new { duracion = tratamiento.DuracionMinutos, precio = Formato.Moneda(tratamiento.Precio), odontologos });
        }

        // Al elegir odontólogo y fecha: los horarios que realmente están libres.
        [HttpGet]
        [Authorize(Roles = AdminOPaciente)]
        public async Task<IActionResult> HorariosDisponibles(int odontologoId, int tratamientoId, DateOnly fecha, int turnoId = 0)
        {
            var duracion = await _context.Tratamientos.AsNoTracking()
                .Where(t => t.Id == tratamientoId).Select(t => (int?)t.DuracionMinutos).FirstOrDefaultAsync();
            if (duracion == null) return NotFound();

            // Más allá de un año no se dan turnos (y una fecha absurda como 9999-12-31 desbordaba los cálculos de fechas).
            if (fecha > Reloj.Hoy.AddYears(1))
                return Json(new { horarios = Array.Empty<string>() });

            var libres = await _disponibilidad.HorariosLibresAsync(odontologoId, fecha, duracion.Value, turnoId == 0 ? null : turnoId);
            return Json(new { horarios = libres.Select(h => h.ToString("HH:mm")) });
        }

        // tratamientoActual: al editar, el tratamiento del turno tiene que seguir en la lista aunque ya no esté disponible.
        private async Task CargarListasAsync(int? tratamientoActual = null)
        {
            if (EsAdmin)
            {
                ViewBag.Pacientes = await _context.Pacientes.AsNoTracking()
                    .OrderBy(p => p.Usuario!.Apellido).ThenBy(p => p.Usuario!.Nombre)
                    .Select(p => new SelectListItem(p.Usuario!.Apellido + ", " + p.Usuario.Nombre + " · DNI " + p.Usuario.Dni, p.Id.ToString()))
                    .ToListAsync();
            }

            var tratamientos = await _context.Tratamientos.AsNoTracking()
                .Where(t => t.Activo || t.Id == tratamientoActual)
                .OrderBy(t => t.Especialidad!.Nombre).ThenBy(t => t.Nombre)
                .Select(t => new { t.Id, t.Nombre, Especialidad = t.Especialidad!.Nombre })
                .ToListAsync();

            // Una sola instancia de grupo por especialidad: así el <select> arma un <optgroup> por especialidad.
            var grupos = tratamientos.Select(t => t.Especialidad).Distinct()
                .ToDictionary(nombre => nombre, nombre => new SelectListGroup { Name = nombre });

            ViewBag.Tratamientos = tratamientos
                .Select(t => new SelectListItem { Text = t.Nombre, Value = t.Id.ToString(), Group = grupos[t.Especialidad] })
                .ToList();
        }
    }
}
