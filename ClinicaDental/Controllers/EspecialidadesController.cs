using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class EspecialidadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EspecialidadesController(ApplicationDbContext context) => _context = context;

        private Task<List<Especialidad>> ListarAsync(string? q = null)
        {
            var consulta = _context.Especialidades.AsNoTracking()
                .Include(e => e.Tratamientos).Include(e => e.Odontologos).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];   // un texto enorme no tiene sentido y rompe la consulta
                consulta = consulta.Where(e => e.Nombre.Contains(texto) || (e.Descripcion != null && e.Descripcion.Contains(texto)));
            }

            return consulta.OrderBy(e => e.Nombre).ToListAsync();
        }

        public async Task<IActionResult> Index() => View(await ListarAsync());

        [HttpGet]
        public async Task<IActionResult> ListarPartial(string? q) =>
            PartialView("_TablaEspecialidadesPartial", await ListarAsync(q));

        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            if (id == 0) return PartialView("_FormularioEspecialidadPartial", new Especialidad());

            var especialidad = await _context.Especialidades.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (especialidad == null) return NotFound();

            return PartialView("_FormularioEspecialidadPartial", especialidad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Especialidad modelo)
        {
            var existente = modelo.Id == 0 ? null : await _context.Especialidades.FindAsync(modelo.Id);
            if (modelo.Id != 0 && existente == null)
                return Json(new { success = false, mensaje = "La especialidad no existe." });

            var nombre = modelo.Nombre?.Trim() ?? string.Empty;
            if (nombre.Length > 0 && await _context.Especialidades.AnyAsync(e => e.Nombre == nombre && e.Id != modelo.Id))
                ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe una especialidad con ese nombre.");

            if (!ModelState.IsValid) return PartialView("_FormularioEspecialidadPartial", modelo);

            if (existente == null)
            {
                modelo.Nombre = nombre;
                _context.Add(modelo);
            }
            else
            {
                // Se copian solo los campos editables: el resto (auditoría, colecciones) no se toca.
                existente.Nombre = nombre;
                existente.Descripcion = modelo.Descripcion;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Especialidad guardada correctamente." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var especialidad = await _context.Especialidades.FindAsync(id);
            if (especialidad == null)
                return Json(new { success = false, mensaje = "La especialidad no existe." });

            if (await _context.Tratamientos.AnyAsync(t => t.EspecialidadId == id) ||
                await _context.Odontologos.AnyAsync(o => o.EspecialidadId == id))
                return Json(new { success = false, mensaje = "La especialidad tiene tratamientos u odontólogos asociados y no puede eliminarse." });

            _context.Especialidades.Remove(especialidad);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Especialidad eliminada correctamente." });
        }
    }
}
