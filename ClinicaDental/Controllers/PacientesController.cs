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
    public class PacientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PacientesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Task<List<Paciente>> ListarAsync(string? q = null, int? obraSocialId = null, string? estado = null)
        {
            var consulta = _context.Pacientes.AsNoTracking()
                .Include(p => p.Usuario).Include(p => p.ObraSocial)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                if (texto.Length > 100) texto = texto[..100];
                consulta = consulta.Where(p => p.Usuario!.Nombre.Contains(texto) || p.Usuario.Apellido.Contains(texto)
                                               || p.Usuario.Email!.Contains(texto) || p.Usuario.Dni.ToString().Contains(texto));
            }
            if (obraSocialId is > 0)
                consulta = consulta.Where(p => p.ObraSocialId == obraSocialId);
            if (obraSocialId == -1)
                consulta = consulta.Where(p => p.ObraSocialId == null);
            if (estado == "activos") consulta = consulta.Where(p => p.Usuario!.Activo);
            if (estado == "inactivos") consulta = consulta.Where(p => !p.Usuario!.Activo);

            return consulta.OrderBy(p => p.Usuario!.Apellido).ThenBy(p => p.Usuario!.Nombre).ToListAsync();
        }

        private async Task CargarObrasSocialesAsync() =>
            ViewBag.ObrasSociales = await _context.ObrasSociales.AsNoTracking().OrderBy(o => o.Nombre)
                .Select(o => new SelectListItem(o.Activa ? o.Nombre : o.Nombre + " (sin convenio)", o.Id.ToString())).ToListAsync();

        public async Task<IActionResult> Index()
        {
            await CargarObrasSocialesAsync();
            return View(await ListarAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarPartial(string? q, int? obraSocialId, string? estado) =>
            PartialView("_TablaPacientesPartial", await ListarAsync(q, obraSocialId, estado));

        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            await CargarObrasSocialesAsync();
            if (id == 0) return PartialView("_FormularioPacientePartial", new PacienteFormViewModel());

            var p = await _context.Pacientes.AsNoTracking().Include(x => x.Usuario).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            return PartialView("_FormularioPacientePartial", new PacienteFormViewModel
            {
                Id = p.Id,
                Nombre = p.Usuario!.Nombre,
                Apellido = p.Usuario.Apellido,
                Dni = p.Usuario.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Email = p.Usuario.Email ?? "",
                Telefono = p.Usuario.PhoneNumber,
                ObraSocialId = p.ObraSocialId,
                NumeroAfiliado = p.NumeroAfiliado,
                Direccion = p.Direccion,
                Alergias = p.Alergias,
                Activo = p.Usuario.Activo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(PacienteFormViewModel vm)
        {
            var esNuevo = vm.Id == 0;
            Paciente? paciente = null;

            if (!esNuevo)
            {
                paciente = await _context.Pacientes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == vm.Id);
                if (paciente == null) return Json(new { success = false, mensaje = "El paciente no existe." });
            }

            var usuarioId = paciente?.UsuarioId;
            var email = vm.Email?.Trim() ?? string.Empty;

            if (esNuevo && string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "La contraseña inicial es obligatoria.");

            if (vm.FechaNacimiento is { } nac && (nac > Reloj.Hoy || nac.Year < 1900))
                ModelState.AddModelError(nameof(vm.FechaNacimiento), "Ingresá una fecha de nacimiento válida.");

            if (email.Length > 0 && await _context.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpper() && u.Id != usuarioId))
                ModelState.AddModelError(nameof(vm.Email), "Ya existe una cuenta con ese correo.");

            if (vm.Dni is { } dni && await _context.Users.AnyAsync(u => u.Dni == dni && u.Id != usuarioId))
                ModelState.AddModelError(nameof(vm.Dni), "Ya existe una persona registrada con ese DNI.");

            if (vm.ObraSocialId is { } osId && !await _context.ObrasSociales.AnyAsync(o => o.Id == osId))
                ModelState.AddModelError(nameof(vm.ObraSocialId), "La obra social elegida no existe.");

            if (!ModelState.IsValid) return await FormularioConErroresAsync(vm);

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

                await _userManager.AddToRoleAsync(usuario, Roles.Paciente);
                _context.Pacientes.Add(new Paciente
                {
                    UsuarioId = usuario.Id, FechaNacimiento = vm.FechaNacimiento!.Value, ObraSocialId = vm.ObraSocialId,
                    NumeroAfiliado = Limpiar(vm.NumeroAfiliado), Direccion = Limpiar(vm.Direccion), Alergias = Limpiar(vm.Alergias)
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                var usuario = paciente!.Usuario!;
                var seDesactiva = usuario.Activo && !vm.Activo;

                usuario.Nombre = vm.Nombre.Trim();
                usuario.Apellido = vm.Apellido.Trim();
                usuario.Dni = vm.Dni!.Value;
                usuario.Email = email;
                usuario.UserName = email;
                usuario.PhoneNumber = vm.Telefono;
                usuario.Activo = vm.Activo;

                paciente.FechaNacimiento = vm.FechaNacimiento!.Value;
                paciente.ObraSocialId = vm.ObraSocialId;
                paciente.NumeroAfiliado = Limpiar(vm.NumeroAfiliado);
                paciente.Direccion = Limpiar(vm.Direccion);
                paciente.Alergias = Limpiar(vm.Alergias);

                var resultado = await _userManager.UpdateAsync(usuario);
                if (!resultado.Succeeded)
                {
                    foreach (var error in resultado.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    return await FormularioConErroresAsync(vm);
                }

                if (seDesactiva) await _userManager.UpdateSecurityStampAsync(usuario);
            }

            await transaccion.CommitAsync();
            return Json(new { success = true, mensaje = "Paciente guardado correctamente." });
        }

        private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private async Task<IActionResult> FormularioConErroresAsync(PacienteFormViewModel vm)
        {
            await CargarObrasSocialesAsync();
            return PartialView("_FormularioPacientePartial", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var paciente = await _context.Pacientes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == id);
            if (paciente == null)
                return Json(new { success = false, mensaje = "El paciente no existe." });

            if (await _context.Turnos.AnyAsync(t => t.PacienteId == id) || await _context.Adjuntos.AnyAsync(a => a.PacienteId == id)
                || await _context.Mensajes.AnyAsync(m => m.PacienteId == id))
                return Json(new { success = false, mensaje = "El paciente tiene turnos o archivos registrados y no puede eliminarse (es parte de su historia clínica). Desactivá su cuenta en su lugar." });

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var usuario = paciente.Usuario!;
            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();
            await _userManager.DeleteAsync(usuario);

            await transaccion.CommitAsync();
            return Json(new { success = true, mensaje = "Paciente eliminado correctamente." });
        }
    }
}
