using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    // Directorio de solo lectura: quiénes atienden, cuándo, y qué tratamientos hacen.
    // La administración de los odontólogos está en OdontologosController.
    [Authorize]
    public class ProfesionalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfesionalesController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // Solo los que hoy pueden recibir turnos: cuenta activa y al menos un horario cargado.
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
