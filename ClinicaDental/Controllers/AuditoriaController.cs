using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    // Registro de quién hizo qué. Se completa solo desde ApplicationDbContext.SaveChanges;
    // acá solo se consulta (no hay alta, edición ni baja: un registro de auditoría no se toca).
    [Authorize(Roles = Roles.Administrador)]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaController(ApplicationDbContext context) => _context = context;

        private Task<List<LogAuditoria>> ListarAsync(string? entidad, string? accion, string? q, DateOnly? desde, DateOnly? hasta)
        {
            var consulta = _context.LogsAuditoria.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(entidad)) consulta = consulta.Where(l => l.Entidad == entidad);
            if (!string.IsNullOrWhiteSpace(accion)) consulta = consulta.Where(l => l.Accion == accion);
            if (desde is { } d) consulta = consulta.Where(l => l.Fecha >= d.ToDateTime(TimeOnly.MinValue));
            if (hasta is { } h) consulta = consulta.Where(l => l.Fecha < h.AddDays(1).ToDateTime(TimeOnly.MinValue));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                consulta = consulta.Where(l => l.UsuarioNombre.Contains(texto) || (l.Detalle != null && l.Detalle.Contains(texto)));
            }

            return consulta.OrderByDescending(l => l.Fecha).ThenByDescending(l => l.Id).Take(300).ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Entidades = await _context.LogsAuditoria.AsNoTracking()
                .Select(l => l.Entidad).Distinct().OrderBy(e => e).ToListAsync();
            return View(await ListarAsync(null, null, null, null, null));
        }

        [HttpGet]
        public async Task<IActionResult> ListarPartial(string? entidad, string? accion, string? q, DateOnly? desde, DateOnly? hasta) =>
            PartialView("_TablaAuditoriaPartial", await ListarAsync(entidad, accion, q, desde, hasta));
    }
}
