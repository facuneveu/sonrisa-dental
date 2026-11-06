using System.Diagnostics;
using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly PerfilService _perfil;

        public HomeController(ApplicationDbContext context, UserManager<Usuario> userManager, PerfilService perfil)
        {
            _context = context;
            _userManager = userManager;
            _perfil = perfil;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true) return View(new InicioViewModel());

            var usuario = await _userManager.GetUserAsync(User);
            var ahora = Reloj.Ahora;
            var hoy = ahora.Date;

            var vm = new InicioViewModel { Nombre = usuario?.Nombre ?? "" };
            var turnos = _context.Turnos.AsNoTracking();

            if (User.IsInRole(Roles.Administrador))
            {
                vm.Rol = "Administrador";
                vm.Indicadores = new()
                {
                    new("Turnos de hoy", await turnos.CountAsync(t => t.FechaHora >= hoy && t.FechaHora < hoy.AddDays(1) && t.Estado != EstadoTurno.Cancelado), "bi-calendar-check", Url.Action("Index", "Turnos", new { periodo = "hoy" })),
                    new("Pendientes de confirmar", await turnos.CountAsync(t => t.Estado == EstadoTurno.Pendiente && t.FechaHora >= ahora), "bi-hourglass-split", Url.Action("Index", "Turnos", new { estado = "Pendiente" })),
                    new("Pacientes activos", await _context.Pacientes.CountAsync(p => p.Usuario!.Activo), "bi-people", Url.Action("Index", "Pacientes")),
                    new("Odontólogos activos", await _context.Odontologos.CountAsync(o => o.Usuario!.Activo), "bi-person-badge", Url.Action("Index", "Odontologos"))
                };
            }
            else if (User.IsInRole(Roles.Odontologo))
            {
                vm.Rol = "Odontólogo/a";
                var id = await _perfil.ObtenerOdontologoIdAsync();
                turnos = turnos.Where(t => t.OdontologoId == id);
                vm.Indicadores = new()
                {
                    new("Turnos de hoy", await turnos.CountAsync(t => t.FechaHora >= hoy && t.FechaHora < hoy.AddDays(1) && t.Estado != EstadoTurno.Cancelado), "bi-calendar-check", Url.Action("Index", "Turnos", new { periodo = "hoy" })),
                    new("Próximos 7 días", await turnos.CountAsync(t => t.FechaHora >= ahora && t.FechaHora < hoy.AddDays(8) && t.Estado != EstadoTurno.Cancelado), "bi-calendar-week", Url.Action("Index", "Turnos", new { periodo = "semana" })),
                    new("Por confirmar", await turnos.CountAsync(t => t.Estado == EstadoTurno.Pendiente && t.FechaHora >= ahora), "bi-hourglass-split", Url.Action("Index", "Turnos", new { estado = "Pendiente" }))
                };
            }
            else
            {
                vm.Rol = "Paciente";
                var id = await _perfil.ObtenerPacienteIdAsync();
                turnos = turnos.Where(t => t.PacienteId == id);
                vm.Indicadores = new()
                {
                    new("Próximos turnos", await turnos.CountAsync(t => t.FechaHora >= ahora && (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado)), "bi-calendar-check", Url.Action("Index", "Turnos")),
                    new("Consultas realizadas", await turnos.CountAsync(t => t.Estado == EstadoTurno.Atendido), "bi-check2-circle", Url.Action("Index", "Turnos", new { estado = "Atendido", periodo = "todos" }))
                };
            }

            vm.ProximosTurnos = await turnos
                .Where(t => t.FechaHora >= ahora && (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado))
                .Include(t => t.Paciente!).ThenInclude(p => p.Usuario)
                .Include(t => t.Odontologo!).ThenInclude(o => o.Usuario)
                .Include(t => t.Tratamiento)
                .OrderBy(t => t.FechaHora)
                .Take(6)
                .ToListAsync();

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
