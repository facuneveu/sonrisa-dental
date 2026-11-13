using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    [Authorize]
    public class TratamientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TratamientosController(ApplicationDbContext context) => _context = context;

        private Task<List<Tratamiento>> ListarAsync(string? q = null, int? especialidadId = null)
        {
            var consulta = _context.Tratamientos.AsNoTracking().Include(t => t.Especialidad).AsQueryable();

            if (!User.IsInRole(Roles.Administrador))
                consulta = consulta.Where(t => t.Activo);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];
                consulta = consulta.Where(t => t.Nombre.Contains(texto) || (t.Descripcion != null && t.Descripcion.Contains(texto)));
            }
            if (especialidadId is > 0)
                consulta = consulta.Where(t => t.EspecialidadId == especialidadId);

            return consulta.OrderBy(t => t.Especialidad!.Nombre).ThenBy(t => t.Nombre).ToListAsync();
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
            PartialView("_TablaTratamientosPartial", await ListarAsync(q, especialidadId));

        [HttpGet]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            await CargarEspecialidadesAsync();
            if (id == 0) return PartialView("_FormularioTratamientoPartial", new Tratamiento());

            var tratamiento = await _context.Tratamientos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (tratamiento == null) return NotFound();

            return PartialView("_FormularioTratamientoPartial", tratamiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Guardar(Tratamiento modelo)
        {
            var existente = modelo.Id == 0 ? null : await _context.Tratamientos.FindAsync(modelo.Id);
            if (modelo.Id != 0 && existente == null)
                return Json(new { success = false, mensaje = "El tratamiento no existe." });

            if (modelo.DuracionMinutos % DisponibilidadService.PasoMinutos != 0)
                ModelState.AddModelError(nameof(modelo.DuracionMinutos), $"La duración debe ser múltiplo de {DisponibilidadService.PasoMinutos} minutos.");

            if (modelo.EspecialidadId > 0 && !await _context.Especialidades.AnyAsync(e => e.Id == modelo.EspecialidadId))
                ModelState.AddModelError(nameof(modelo.EspecialidadId), "La especialidad elegida no existe.");

            var nombre = modelo.Nombre?.Trim() ?? string.Empty;
            if (nombre.Length > 0 && await _context.Tratamientos.AnyAsync(t => t.Nombre == nombre && t.EspecialidadId == modelo.EspecialidadId && t.Id != modelo.Id))
                ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe un tratamiento con ese nombre en la especialidad.");

            if (!ModelState.IsValid)
            {
                await CargarEspecialidadesAsync();
                return PartialView("_FormularioTratamientoPartial", modelo);
            }

            if (existente == null)
            {
                modelo.Nombre = nombre;
                _context.Add(modelo);
            }
            else
            {
                existente.Nombre = nombre;
                existente.Descripcion = modelo.Descripcion;
                existente.DuracionMinutos = modelo.DuracionMinutos;
                existente.Precio = modelo.Precio;
                existente.EspecialidadId = modelo.EspecialidadId;
                existente.Activo = modelo.Activo;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Tratamiento guardado correctamente." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var tratamiento = await _context.Tratamientos.FindAsync(id);
            if (tratamiento == null)
                return Json(new { success = false, mensaje = "El tratamiento no existe." });

            if (await _context.Turnos.AnyAsync(t => t.TratamientoId == id))
                return Json(new { success = false, mensaje = "El tratamiento tiene turnos asociados. Desmarcalo como disponible en lugar de eliminarlo." });

            _context.Tratamientos.Remove(tratamiento);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Tratamiento eliminado correctamente." });
        }
    }
}
