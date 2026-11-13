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
    // Ficha clínica de un paciente: consultas (historia clínica), odontograma y archivos.
    //   Paciente   -> ve la suya (solo lectura)
    //   Odontólogo -> ve y completa la de los pacientes con los que tiene turnos
    //   Administración -> sin acceso (gestiona turnos y personas, no contenido clínico)
    // Cada acción comprueba el acceso al paciente en el servidor; un id cambiado a mano en la URL no sirve.
    [Authorize(Roles = Roles.OdontologoOPaciente)]
    public class FichaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PerfilService _perfil;
        private readonly ArchivoService _archivos;

        public FichaController(ApplicationDbContext context, PerfilService perfil, ArchivoService archivos)
        {
            _context = context;
            _perfil = perfil;
            _archivos = archivos;
        }

        private JsonResult Ok(string mensaje) => Json(new { success = true, mensaje });
        private JsonResult Falla(string mensaje) => Json(new { success = false, mensaje });
        private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        // ------------------------------------------------------------------ Mis pacientes (odontólogo)

        // El paciente no tiene listado: va directo a su propia ficha.
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(Roles.Paciente))
            {
                var id = await _perfil.ObtenerPacienteIdAsync();
                return id == null ? NotFound() : RedirectToAction(nameof(Detalle), new { id });
            }
            return View(await ListarPacientesAsync(null));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> ListarPartial(string? q) =>
            PartialView("_TablaMisPacientesPartial", await ListarPacientesAsync(q));

        private async Task<List<PacienteDelOdontologo>> ListarPacientesAsync(string? q)
        {
            var ahora = Reloj.Ahora;
            var hoy = Reloj.Hoy;
            var miId = await _perfil.ObtenerOdontologoIdAsync();

            var turnos = await _context.Turnos.AsNoTracking().Where(t => t.OdontologoId == miId)
                .Select(t => new { t.PacienteId, t.FechaHora, t.Estado }).ToListAsync();
            var porPaciente = turnos.GroupBy(t => t.PacienteId).ToDictionary(g => g.Key, g => g.ToList());

            var ids = porPaciente.Keys.ToList();
            var consulta = _context.Pacientes.AsNoTracking().Include(p => p.Usuario).Include(p => p.ObraSocial).Where(p => ids.Contains(p.Id));
            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];   // un texto enorme no tiene sentido y rompe la consulta
                consulta = consulta.Where(p => p.Usuario!.Nombre.Contains(texto) || p.Usuario.Apellido.Contains(texto) || p.Usuario.Dni.ToString().Contains(texto));
            }
            var pacientes = await consulta.OrderBy(p => p.Usuario!.Apellido).ThenBy(p => p.Usuario!.Nombre).ToListAsync();

            return pacientes.Select(p =>
            {
                var suyos = porPaciente[p.Id];
                return new PacienteDelOdontologo(p.Id, p.Usuario!.NombreCompleto, Edad(p.FechaNacimiento, hoy),
                    p.ObraSocial?.Nombre ?? "Particular", p.Alergias, suyos.Count,
                    suyos.Where(t => t.Estado == EstadoTurno.Atendido).Select(t => (DateTime?)t.FechaHora).Max(),
                    suyos.Where(t => t.FechaHora >= ahora && (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado))
                         .Select(t => (DateTime?)t.FechaHora).Min());
            }).ToList();
        }

        private static int Edad(DateOnly nacimiento, DateOnly hoy) =>
            hoy.Year - nacimiento.Year - (hoy < nacimiento.AddYears(hoy.Year - nacimiento.Year) ? 1 : 0);

        // ------------------------------------------------------------------ Ficha

        public async Task<IActionResult> Detalle(int id)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(id)) return Forbid();

            var vm = await ConstruirAsync(id);
            return vm == null ? NotFound() : View(vm);
        }

        // Solo el contenido, para refrescar la ficha después de guardar sin recargar la página.
        [HttpGet]
        public async Task<IActionResult> ContenidoPartial(int id)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(id)) return Forbid();

            var vm = await ConstruirAsync(id);
            return vm == null ? NotFound() : PartialView("_FichaContenidoPartial", vm);
        }

        private async Task<FichaViewModel?> ConstruirAsync(int pacienteId)
        {
            var paciente = await _context.Pacientes.AsNoTracking()
                .Include(p => p.Usuario).Include(p => p.ObraSocial)
                .FirstOrDefaultAsync(p => p.Id == pacienteId);
            if (paciente == null) return null;

            var esOdontologo = User.IsInRole(Roles.Odontologo);
            var miId = esOdontologo ? await _perfil.ObtenerOdontologoIdAsync() : null;

            var vm = new FichaViewModel
            {
                Paciente = paciente,
                Edad = Edad(paciente.FechaNacimiento, Reloj.Hoy),
                PuedeEditar = esOdontologo
            };

            vm.Consultas = await _context.HistoriasClinicas.AsNoTracking()
                .Where(h => h.Turno!.PacienteId == pacienteId)
                .OrderByDescending(h => h.Turno!.FechaHora)
                .Select(h => new ConsultaItem(h.Id, h.TurnoId, h.Turno!.FechaHora, h.Turno.Tratamiento!.Nombre,
                    h.Turno.Odontologo!.Usuario!.Nombre + " " + h.Turno.Odontologo.Usuario.Apellido,
                    h.Turno.OdontologoId == miId, h.Diagnostico, h.Procedimiento, h.Indicaciones, h.Adjuntos.Count))
                .ToListAsync();

            if (esOdontologo)
            {
                vm.Pendientes = await _context.Turnos.AsNoTracking()
                    .Where(t => t.PacienteId == pacienteId && t.OdontologoId == miId && t.Estado == EstadoTurno.Atendido && t.HistoriaClinica == null)
                    .OrderByDescending(t => t.FechaHora)
                    .Select(t => new ConsultaPendiente(t.Id, t.FechaHora, t.Tratamiento!.Nombre))
                    .ToListAsync();
            }

            vm.Piezas = await _context.PiezasDentales.AsNoTracking()
                .Where(p => p.PacienteId == pacienteId).ToDictionaryAsync(p => p.NumeroPieza);

            // Sin cargar el contenido: solo los datos de cada archivo.
            vm.Adjuntos = await _context.Adjuntos.AsNoTracking()
                .Where(a => a.PacienteId == pacienteId)
                .OrderByDescending(a => a.FechaCreacion)
                .Select(a => new AdjuntoResumen(a.Id, a.Tipo, a.NombreOriginal, a.TipoContenido, a.TamanoBytes, a.Descripcion,
                    a.HistoriaClinicaId, a.FechaCreacion, a.UsuarioCreacionId))
                .ToListAsync();

            return vm;
        }

        // ------------------------------------------------------------------ Historia clínica (odontólogo)

        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> FormularioHistoria(int turnoId)
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();
            var turno = await _context.Turnos.AsNoTracking()
                .Include(t => t.Paciente!).ThenInclude(p => p.Usuario).Include(t => t.Tratamiento)
                .FirstOrDefaultAsync(t => t.Id == turnoId && t.OdontologoId == miId);
            if (turno == null) return NotFound();

            if (turno.Estado != EstadoTurno.Atendido)
                return PartialView("_AvisoModalPartial", ("Todavía no se puede cargar", "Marcá el turno como atendido y después completá la historia clínica."));

            ViewBag.Turno = turno;
            var historia = await _context.HistoriasClinicas.AsNoTracking().FirstOrDefaultAsync(h => h.TurnoId == turnoId)
                           ?? new HistoriaClinica { TurnoId = turnoId };
            return PartialView("_FormularioHistoriaPartial", historia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> GuardarHistoria(HistoriaClinica modelo)
        {
            var miId = await _perfil.ObtenerOdontologoIdAsync();
            var turno = await _context.Turnos.Include(t => t.Paciente!).ThenInclude(p => p.Usuario).Include(t => t.Tratamiento)
                .FirstOrDefaultAsync(t => t.Id == modelo.TurnoId && t.OdontologoId == miId);
            if (turno == null) return Falla("El turno no existe o no es de tu agenda.");
            if (turno.Estado != EstadoTurno.Atendido) return Falla("Solo se puede cargar la historia clínica de un turno atendido.");

            if (!ModelState.IsValid)
            {
                ViewBag.Turno = turno;
                return PartialView("_FormularioHistoriaPartial", modelo);
            }

            // Se copian solo los campos editables: lo demás (turno, auditoría) no viene del formulario.
            var historia = await _context.HistoriasClinicas.FirstOrDefaultAsync(h => h.TurnoId == turno.Id);
            if (historia == null)
            {
                historia = new HistoriaClinica { TurnoId = turno.Id };
                _context.HistoriasClinicas.Add(historia);
            }
            historia.Diagnostico = modelo.Diagnostico.Trim();
            historia.Procedimiento = Limpiar(modelo.Procedimiento);
            historia.Indicaciones = Limpiar(modelo.Indicaciones);

            await _context.SaveChangesAsync();
            return Ok("Historia clínica guardada correctamente.");
        }

        // ------------------------------------------------------------------ Odontograma (odontólogo)

        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> FormularioPieza(int pacienteId, int numero)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(pacienteId)) return Forbid();
            if (!PiezaDental.EsNumeroFdiValido(numero)) return NotFound();

            var existente = await _context.PiezasDentales.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PacienteId == pacienteId && p.NumeroPieza == numero);

            return PartialView("_FormularioPiezaPartial", new PiezaFormViewModel
            {
                PacienteId = pacienteId,
                NumeroPieza = numero,
                Estado = existente?.Estado ?? EstadoPieza.Sana,
                Observaciones = existente?.Observaciones
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> GuardarPieza(PiezaFormViewModel vm)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(vm.PacienteId)) return Falla("No tenés acceso a este paciente.");
            if (!ModelState.IsValid) return PartialView("_FormularioPiezaPartial", vm);

            var existente = await _context.PiezasDentales
                .FirstOrDefaultAsync(p => p.PacienteId == vm.PacienteId && p.NumeroPieza == vm.NumeroPieza);
            var observaciones = Limpiar(vm.Observaciones);

            // Una pieza sana y sin notas es el estado por defecto: no hace falta guardar una fila.
            if (vm.Estado == EstadoPieza.Sana && observaciones == null)
            {
                if (existente != null) _context.PiezasDentales.Remove(existente);
            }
            else if (existente == null)
            {
                _context.PiezasDentales.Add(new PiezaDental
                {
                    PacienteId = vm.PacienteId, NumeroPieza = vm.NumeroPieza, Estado = vm.Estado, Observaciones = observaciones
                });
            }
            else
            {
                existente.Estado = vm.Estado;
                existente.Observaciones = observaciones;
            }

            await _context.SaveChangesAsync();
            return Ok($"Pieza {vm.NumeroPieza} actualizada.");
        }

        // ------------------------------------------------------------------ Archivos

        [HttpGet]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> FormularioAdjunto(int pacienteId)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(pacienteId)) return Forbid();
            await CargarConsultasAsync(pacienteId);
            return PartialView("_FormularioAdjuntoPartial", new AdjuntoFormViewModel { PacienteId = pacienteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> GuardarAdjunto(AdjuntoFormViewModel vm)
        {
            if (!await _perfil.PuedeAccederAFichaAsync(vm.PacienteId)) return Falla("No tenés acceso a este paciente.");

            if (vm.HistoriaClinicaId is { } hid
                && !await _context.HistoriasClinicas.AnyAsync(h => h.Id == hid && h.Turno!.PacienteId == vm.PacienteId))
                ModelState.AddModelError(nameof(vm.HistoriaClinicaId), "La consulta elegida no corresponde a este paciente.");

            var archivo = await _archivos.LeerYValidarAsync(vm.Archivo);
            if (!archivo.Ok) ModelState.AddModelError(nameof(vm.Archivo), archivo.Error!);

            if (!ModelState.IsValid)
            {
                await CargarConsultasAsync(vm.PacienteId);
                return PartialView("_FormularioAdjuntoPartial", vm);
            }

            _context.Adjuntos.Add(new Adjunto
            {
                PacienteId = vm.PacienteId,
                HistoriaClinicaId = vm.HistoriaClinicaId,
                Tipo = vm.Tipo,
                NombreOriginal = archivo.NombreOriginal,
                TipoContenido = archivo.TipoContenido,
                TamanoBytes = archivo.Contenido.Length,
                Contenido = archivo.Contenido,
                Descripcion = Limpiar(vm.Descripcion)
            });
            await _context.SaveChangesAsync();
            return Ok("Archivo subido correctamente.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Odontologo)]
        public async Task<IActionResult> EliminarAdjunto(int id)
        {
            var adjunto = await _context.Adjuntos.FindAsync(id);
            if (adjunto == null) return Falla("El archivo no existe.");
            if (!await _perfil.PuedeAccederAFichaAsync(adjunto.PacienteId)) return Falla("No tenés acceso a este paciente.");
            if (adjunto.UsuarioCreacionId != _perfil.UsuarioId) return Falla("Solo quien subió el archivo puede eliminarlo.");

            _context.Adjuntos.Remove(adjunto);
            await _context.SaveChangesAsync();
            return Ok("Archivo eliminado correctamente.");
        }

        // El archivo se sirve desde acá (nunca desde una carpeta pública) para poder comprobar quién lo pide.
        [HttpGet]
        public async Task<IActionResult> Archivo(int id, bool descargar = false)
        {
            var adjunto = await _context.Adjuntos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (adjunto == null) return NotFound();
            if (!await _perfil.PuedeAccederAFichaAsync(adjunto.PacienteId)) return Forbid();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "private, no-store";
            return descargar
                ? File(adjunto.Contenido, adjunto.TipoContenido, adjunto.NombreOriginal)
                : File(adjunto.Contenido, adjunto.TipoContenido);
        }

        private async Task CargarConsultasAsync(int pacienteId)
        {
            var consultas = await _context.HistoriasClinicas.AsNoTracking()
                .Where(h => h.Turno!.PacienteId == pacienteId)
                .OrderByDescending(h => h.Turno!.FechaHora)
                .Select(h => new { h.Id, h.Turno!.FechaHora, Tratamiento = h.Turno.Tratamiento!.Nombre })
                .ToListAsync();

            ViewBag.Consultas = consultas
                .Select(c => new SelectListItem($"{Formato.Fecha(c.FechaHora)} · {c.Tratamiento}", c.Id.ToString()))
                .ToList();
        }
    }
}
