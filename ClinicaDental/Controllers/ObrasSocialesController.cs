using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class ObrasSocialesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ObrasSocialesController(ApplicationDbContext context) => _context = context;

        private Task<List<ObraSocial>> ListarAsync(string? q = null, string? estado = null)
        {
            var consulta = _context.ObrasSociales.AsNoTracking().Include(o => o.Pacientes).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];
                consulta = consulta.Where(o => o.Nombre.Contains(texto) || (o.Cobertura != null && o.Cobertura.Contains(texto)));
            }
            if (estado == "activas") consulta = consulta.Where(o => o.Activa);
            if (estado == "inactivas") consulta = consulta.Where(o => !o.Activa);

            return consulta.OrderBy(o => o.Nombre).ToListAsync();
        }

        public async Task<IActionResult> Index() => View(await ListarAsync());

        [HttpGet]
        public async Task<IActionResult> ListarPartial(string? q, string? estado) =>
            PartialView("_TablaObrasSocialesPartial", await ListarAsync(q, estado));

        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            if (id == 0) return PartialView("_FormularioObraSocialPartial", new ObraSocial());

            var obra = await _context.ObrasSociales.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (obra == null) return NotFound();

            return PartialView("_FormularioObraSocialPartial", obra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(ObraSocial modelo)
        {
            var existente = modelo.Id == 0 ? null : await _context.ObrasSociales.FindAsync(modelo.Id);
            if (modelo.Id != 0 && existente == null)
                return Json(new { success = false, mensaje = "La obra social no existe." });

            var nombre = modelo.Nombre?.Trim() ?? string.Empty;
            if (nombre.Length > 0 && await _context.ObrasSociales.AnyAsync(o => o.Nombre == nombre && o.Id != modelo.Id))
                ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe una obra social con ese nombre.");

            if (!ModelState.IsValid) return PartialView("_FormularioObraSocialPartial", modelo);

            if (existente == null)
            {
                modelo.Nombre = nombre;
                _context.Add(modelo);
            }
            else
            {
                existente.Nombre = nombre;
                existente.Telefono = modelo.Telefono;
                existente.SitioWeb = modelo.SitioWeb;
                existente.Cobertura = modelo.Cobertura;
                existente.Activa = modelo.Activa;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Obra social guardada correctamente." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var obra = await _context.ObrasSociales.FindAsync(id);
            if (obra == null)
                return Json(new { success = false, mensaje = "La obra social no existe." });

            if (await _context.Pacientes.AnyAsync(p => p.ObraSocialId == id))
                return Json(new { success = false, mensaje = "Hay pacientes asociados a esta obra social. Marcala como sin convenio vigente en lugar de eliminarla." });

            _context.ObrasSociales.Remove(obra);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Obra social eliminada correctamente." });
        }
    }
}
