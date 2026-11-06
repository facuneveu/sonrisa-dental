using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class OdontologosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public OdontologosController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Task<List<Odontologo>> ListarAsync(string? q = null, int? especialidadId = null)
        {
            var consulta = _context.Odontologos.AsNoTracking()
                .Include(o => o.Usuario).Include(o => o.Especialidad).Include(o => o.Horarios)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                consulta = consulta.Where(o => o.Usuario!.Nombre.Contains(texto) || o.Usuario.Apellido.Contains(texto)
                                               || o.Matricula.Contains(texto) || o.Usuario.Email!.Contains(texto));
            }
            if (especialidadId is > 0)
                consulta = consulta.Where(o => o.EspecialidadId == especialidadId);

            return consulta.OrderBy(o => o.Usuario!.Apellido).ThenBy(o => o.Usuario!.Nombre).ToListAsync();
        }

        private async Task CargarEspecialidadesAsync() =>
            ViewBag.Especialidades = await _context.Especialidades.AsNoTracking().OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem(e.Nombre, e.Id.ToString())).ToListAsync();

        public async Task<IActionResult> Index()
        {
            await CargarEspecialidadesAsync();
            return View(await ListarAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarPartial(string? q, int? especialidadId) =>
            PartialView("_TablaOdontologosPartial", await ListarAsync(q, especialidadId));

        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            await CargarEspecialidadesAsync();
            if (id == 0) return PartialView("_FormularioOdontologoPartial", new OdontologoFormViewModel());

            var o = await _context.Odontologos.AsNoTracking().Include(x => x.Usuario).FirstOrDefaultAsync(x => x.Id == id);
            if (o == null) return NotFound();

            return PartialView("_FormularioOdontologoPartial", new OdontologoFormViewModel
            {
                Id = o.Id,
                Nombre = o.Usuario!.Nombre,
                Apellido = o.Usuario.Apellido,
                Dni = o.Usuario.Dni,
                Email = o.Usuario.Email ?? "",
                Telefono = o.Usuario.PhoneNumber,
                Matricula = o.Matricula,
                EspecialidadId = o.EspecialidadId,
                Presentacion = o.Presentacion,
                Activo = o.Usuario.Activo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(OdontologoFormViewModel vm)
        {
            var esNuevo = vm.Id == 0;
            Odontologo? odontologo = null;

            if (!esNuevo)
            {
                odontologo = await _context.Odontologos.Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == vm.Id);
                if (odontologo == null) return Json(new { success = false, mensaje = "El odontólogo no existe." });
            }

            var usuarioId = odontologo?.UsuarioId;
            var email = vm.Email?.Trim() ?? string.Empty;
            var matricula = vm.Matricula?.Trim() ?? string.Empty;

            if (esNuevo && string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "La contraseña inicial es obligatoria.");

            if (email.Length > 0 && await _context.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpper() && u.Id != usuarioId))
                ModelState.AddModelError(nameof(vm.Email), "Ya existe una cuenta con ese correo.");

            if (vm.Dni is { } dni && await _context.Users.AnyAsync(u => u.Dni == dni && u.Id != usuarioId))
                ModelState.AddModelError(nameof(vm.Dni), "Ya existe una persona registrada con ese DNI.");

            if (matricula.Length > 0 && await _context.Odontologos.AnyAsync(o => o.Matricula == matricula && o.Id != vm.Id))
                ModelState.AddModelError(nameof(vm.Matricula), "Ya existe un odontólogo con esa matrícula.");

            if (vm.EspecialidadId > 0 && !await _context.Especialidades.AnyAsync(e => e.Id == vm.EspecialidadId))
                ModelState.AddModelError(nameof(vm.EspecialidadId), "La especialidad elegida no existe.");

            if (!ModelState.IsValid) return await FormularioConErroresAsync(vm);

            // Cuenta y perfil se guardan juntos: si algo falla, no queda un usuario sin perfil.
            await using var transaccion = await _context.Database.BeginTransactionAsync();

            if (esNuevo)
            {
                var usuario = new Usuario
                {
                    UserName = email, Email = email, EmailConfirmed = true, PhoneNumber = vm.Telefono,
                    Nombre = vm.Nombre.Trim(), Apellido = vm.Apellido.Trim(), Dni = vm.Dni!.Value, Activo = vm.Activo
                };

                var resultado = await _userManager.CreateAsync(usuario, vm.Password!);
                if (!resultado.Succeeded)
                {
                    foreach (var error in resultado.Errors)
                        ModelState.AddModelError(error.Code.StartsWith("Password") ? nameof(vm.Password) : string.Empty, error.Description);
                    return await FormularioConErroresAsync(vm);
                }

                await _userManager.AddToRoleAsync(usuario, Roles.Odontologo);
                _context.Odontologos.Add(new Odontologo
                {
                    UsuarioId = usuario.Id, Matricula = matricula,
                    EspecialidadId = vm.EspecialidadId, Presentacion = vm.Presentacion
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                var usuario = odontologo!.Usuario!;
                var seDesactiva = usuario.Activo && !vm.Activo;

                usuario.Nombre = vm.Nombre.Trim();
                usuario.Apellido = vm.Apellido.Trim();
                usuario.Dni = vm.Dni!.Value;
                usuario.Email = email;
                usuario.UserName = email;
                usuario.PhoneNumber = vm.Telefono;
                usuario.Activo = vm.Activo;

                odontologo.Matricula = matricula;
                odontologo.EspecialidadId = vm.EspecialidadId;
                odontologo.Presentacion = vm.Presentacion;

                // UpdateAsync también guarda los cambios pendientes del perfil (mismo DbContext).
                var resultado = await _userManager.UpdateAsync(usuario);
                if (!resultado.Succeeded)
                {
                    foreach (var error in resultado.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    return await FormularioConErroresAsync(vm);
                }

                // Cambiar el sello de seguridad invalida las sesiones ya abiertas de esa cuenta.
                if (seDesactiva) await _userManager.UpdateSecurityStampAsync(usuario);
            }

            await transaccion.CommitAsync();
            return Json(new { success = true, mensaje = "Odontólogo guardado correctamente." });
        }

        private async Task<IActionResult> FormularioConErroresAsync(OdontologoFormViewModel vm)
        {
            await CargarEspecialidadesAsync();
            return PartialView("_FormularioOdontologoPartial", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var odontologo = await _context.Odontologos.Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == id);
            if (odontologo == null)
                return Json(new { success = false, mensaje = "El odontólogo no existe." });

            if (await _context.Turnos.AnyAsync(t => t.OdontologoId == id))
                return Json(new { success = false, mensaje = "El odontólogo tiene turnos registrados y no puede eliminarse. Desactivá su cuenta para que deje de recibir turnos." });

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var usuario = odontologo.Usuario!;
            _context.Odontologos.Remove(odontologo);   // sus horarios se eliminan en cascada
            await _context.SaveChangesAsync();
            await _userManager.DeleteAsync(usuario);

            await transaccion.CommitAsync();
            return Json(new { success = true, mensaje = "Odontólogo eliminado correctamente." });
        }

        // ---------------------------------------------------------------------------------------
        // Horarios de atención (se administran desde un modal propio de cada odontólogo)
        // ---------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Horarios(int id)
        {
            var odontologo = await _context.Odontologos.AsNoTracking()
                .Include(o => o.Usuario).Include(o => o.Horarios)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (odontologo == null) return NotFound();

            return PartialView("_HorariosOdontologoPartial", odontologo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarHorario(HorarioFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, mensaje = "Completá el día y las horas de inicio y fin." });

            if (!await _context.Odontologos.AnyAsync(o => o.Id == vm.OdontologoId))
                return Json(new { success = false, mensaje = "El odontólogo no existe." });

            var desde = vm.HoraDesde!.Value;
            var hasta = vm.HoraHasta!.Value;

            if (hasta <= desde)
                return Json(new { success = false, mensaje = "La hora de fin debe ser posterior a la de inicio." });

            if (desde.Minute % DisponibilidadService.PasoMinutos != 0 || hasta.Minute % DisponibilidadService.PasoMinutos != 0)
                return Json(new { success = false, mensaje = $"Las horas deben ser múltiplos de {DisponibilidadService.PasoMinutos} minutos (por ejemplo 09:00, 09:15, 09:30)." });

            var existentes = await _context.HorariosAtencion
                .Where(h => h.OdontologoId == vm.OdontologoId && h.DiaSemana == vm.DiaSemana).ToListAsync();
            if (existentes.Any(h => desde < h.HoraHasta && hasta > h.HoraDesde))
                return Json(new { success = false, mensaje = "Esa franja se superpone con otra que ya está cargada para ese día." });

            _context.HorariosAtencion.Add(new HorarioAtencion
            {
                OdontologoId = vm.OdontologoId, DiaSemana = vm.DiaSemana, HoraDesde = desde, HoraHasta = hasta
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Horario agregado." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarHorario(int id)
        {
            var horario = await _context.HorariosAtencion.FindAsync(id);
            if (horario == null)
                return Json(new { success = false, mensaje = "El horario no existe." });

            // No se puede quitar una franja si hay turnos futuros que dependen de ella.
            var futuros = await _context.Turnos.AsNoTracking()
                .Where(t => t.OdontologoId == horario.OdontologoId && t.FechaHora >= Reloj.Ahora && t.Estado != EstadoTurno.Cancelado)
                .Select(t => t.FechaHora).ToListAsync();

            if (futuros.Any(f => f.DayOfWeek == horario.DiaSemana
                                 && TimeOnly.FromDateTime(f) >= horario.HoraDesde && TimeOnly.FromDateTime(f) < horario.HoraHasta))
                return Json(new { success = false, mensaje = "Hay turnos futuros dentro de esta franja. Reprogramalos o cancelalos antes de quitarla." });

            _context.HorariosAtencion.Remove(horario);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Horario eliminado." });
        }
    }
}
