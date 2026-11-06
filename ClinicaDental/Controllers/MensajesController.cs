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
    // Avisos del odontólogo al paciente ante cualquier inconveniente (demora, reprogramación, cancelación...).
    //   Odontólogo -> envía, solo a pacientes con los que tiene turnos, y ve si fueron leídos
    //   Paciente   -> recibe en su bandeja (no responde por acá: puede cancelar o sacar otro turno)
    [Authorize(Roles = Roles.OdontologoOPaciente)]
    public class MensajesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PerfilService _perfil;

        public MensajesController(ApplicationDbContext context, PerfilService perfil)
        {
            _context = context;
            _perfil = perfil;
        }

        // ------------------------------------------------------------------ Bandeja

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(Roles.Paciente))
            {
                var pacienteId = await _perfil.ObtenerPacienteIdAsync();

                // Sin AsNoTracking: al abrir la bandeja los mensajes pasan a leídos.
                var mensajes = await _context.Mensajes
                    .Include(m => m.Odontologo!).ThenInclude(o => o.Usuario)
                    .Include(m => m.Turno!).ThenInclude(t => t.Tratamiento)
                    .Where(m => m.PacienteId == pacienteId)
                    .OrderByDescending(m => m.FechaEnvio).Take(100)
                    .ToListAsync();

                // Los que llegaron sin leer se marcan como "nuevos" en esta vista y recién después se registran como leídos.
                var nuevos = mensajes.Where(m => !m.Leido).Select(m => m.Id).ToHashSet();
                ViewBag.Nuevos = nuevos;

                if (nuevos.Count > 0)
                {
                    var ahora = Reloj.Ahora;
                    foreach (var m in mensajes.Where(m => nuevos.Contains(m.Id))) m.FechaLectura = ahora;
                    await _context.SaveChangesAsync();
                }

                return View(mensajes);
            }

            return View(await EnviadosAsync());
        }

        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> EnviadosPartial() => PartialView("_TablaEnviadosPartial", await EnviadosAsync());

        private async Task<List<Mensaje>> EnviadosAsync()
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();
            return await _context.Mensajes.AsNoTracking()
                .Include(m => m.Paciente!).ThenInclude(p => p.Usuario)
                .Include(m => m.Turno!).ThenInclude(t => t.Tratamiento)
                .Where(m => m.OdontologoId == miId)
                .OrderByDescending(m => m.FechaEnvio).Take(100)
                .ToListAsync();
        }

        // ------------------------------------------------------------------ Envío (odontólogo)

        // pacienteId / turnoId llegan cuando se abre desde un turno, desde la ficha o al cancelar.
        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> Formulario(int pacienteId = 0, int turnoId = 0, TipoMensaje tipo = TipoMensaje.Demora)
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();

            // Si se abre desde un turno, el paciente sale del turno (y se valida que sea de este odontólogo).
            if (turnoId > 0)
            {
                var turno = await CargarTurnoDelFormularioAsync(turnoId, miId);
                if (turno == null) return NotFound();
                pacienteId = turno.PacienteId;
            }

            await CargarPacientesAsync(miId);
            return PartialView("_FormularioMensajePartial", new MensajeFormViewModel
            {
                PacienteId = pacienteId,
                TurnoId = turnoId > 0 ? turnoId : null,
                Tipo = tipo
            });
        }

        // Turnos vigentes de un paciente con este odontólogo, para relacionar el aviso con uno de ellos.
        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> TurnosDelPaciente(int pacienteId)
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();
            var turnos = await _context.Turnos.AsNoTracking()
                .Where(t => t.OdontologoId == miId && t.PacienteId == pacienteId
                            && t.FechaHora >= Reloj.Ahora && t.Estado != EstadoTurno.Cancelado)
                .OrderBy(t => t.FechaHora)
                .Select(t => new { t.Id, t.FechaHora, Tratamiento = t.Tratamiento!.Nombre })
                .ToListAsync();

            return Json(turnos.Select(t => new { id = t.Id, texto = $"{Formato.DiaYFecha(t.FechaHora)} {t.FechaHora:HH:mm} · {t.Tratamiento}" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> Enviar(MensajeFormViewModel vm)
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();
            if (miId == null) return Json(new { success = false, mensaje = "Tu cuenta no tiene un perfil de odontólogo." });

            // Solo se le escribe a pacientes con los que se tiene (o se tuvo) un turno.
            Paciente? paciente = null;
            if (vm.PacienteId > 0 && await _perfil.PuedeAccederAFichaAsync(vm.PacienteId))
                paciente = await _context.Pacientes.AsNoTracking().Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == vm.PacienteId);
            if (paciente == null)
                ModelState.AddModelError(nameof(vm.PacienteId), "Elegí un paciente de tu agenda.");

            if (vm.TurnoId is { } tid
                && !await _context.Turnos.AnyAsync(t => t.Id == tid && t.OdontologoId == miId && t.PacienteId == vm.PacienteId))
                ModelState.AddModelError(nameof(vm.TurnoId), "El turno elegido no corresponde a ese paciente.");

            if (!ModelState.IsValid)
            {
                // Si el turno es válido, el formulario vuelve "atado" a él; si no, vuelve al modo general
                // (con la lista de pacientes y el error visible) para que se pueda corregir.
                if (vm.TurnoId is { } turnoId && await CargarTurnoDelFormularioAsync(turnoId, miId) == null)
                    vm.TurnoId = null;

                await CargarPacientesAsync(miId);
                return PartialView("_FormularioMensajePartial", vm);
            }

            _context.Mensajes.Add(new Mensaje
            {
                OdontologoId = miId.Value,
                PacienteId = vm.PacienteId,
                TurnoId = vm.TurnoId,
                Tipo = vm.Tipo,
                Asunto = vm.Asunto.Trim(),
                Texto = vm.Texto.Trim(),
                FechaEnvio = Reloj.Ahora
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, mensaje = $"Mensaje enviado a {paciente!.Usuario!.NombreYApellido}." });
        }

        // Carga en el ViewBag los textos que muestra el formulario cuando se abre sobre un turno.
        private async Task<Turno?> CargarTurnoDelFormularioAsync(int turnoId, int? odontologoId)
        {
            var turno = await _context.Turnos.AsNoTracking()
                .Include(t => t.Paciente!).ThenInclude(p => p.Usuario).Include(t => t.Tratamiento)
                .FirstOrDefaultAsync(t => t.Id == turnoId && t.OdontologoId == odontologoId);
            if (turno == null) return null;

            ViewBag.PacienteNombre = turno.Paciente!.Usuario!.NombreCompleto;
            ViewBag.TurnoTexto = $"{Formato.DiaYFecha(turno.FechaHora)} {turno.FechaHora:HH:mm} · {turno.Tratamiento!.Nombre}";
            ViewBag.NombrePila = turno.Paciente.Usuario.Nombre;
            return turno;
        }

        private async Task CargarPacientesAsync(int? odontologoId)
        {
            var ids = await _context.Turnos.AsNoTracking().Where(t => t.OdontologoId == odontologoId)
                .Select(t => t.PacienteId).Distinct().ToListAsync();

            ViewBag.Pacientes = await _context.Pacientes.AsNoTracking().Where(p => ids.Contains(p.Id))
                .OrderBy(p => p.Usuario!.Apellido).ThenBy(p => p.Usuario!.Nombre)
                .Select(p => new SelectListItem(p.Usuario!.Apellido + ", " + p.Usuario.Nombre, p.Id.ToString()))
                .ToListAsync();
        }
    }
}
