using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    [Authorize]
    public class ProfesionalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfesionalesController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var profesionales = await _context.Odontologos.AsNoTracking()
                .Include(o => o.Usuario).Include(o => o.Especialidad).Include(o => o.Horarios)
                .Where(o => o.Usuario!.Activo && o.Horarios.Any())
                .OrderBy(o => o.Usuario!.Apellido).ThenBy(o => o.Usuario!.Nombre)
                .ToListAsync();

            ViewBag.Tratamientos = await _context.Tratamientos.AsNoTracking()
                .Where(t => t.Activo).OrderBy(t => t.Nombre).ToListAsync();

            return View(profesionales);
        }
    }
}
