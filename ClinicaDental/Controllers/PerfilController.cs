using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Controllers
{
    // "Mi perfil": cada persona ve sus datos y cambia lo que le corresponde (contacto y contraseña).
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public PerfilController(ApplicationDbContext context, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = await ConstruirAsync();
            return vm == null ? Challenge() : View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDatos([Bind(Prefix = "Datos")] PerfilFormViewModel datos)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var paciente = User.IsInRole(Roles.Paciente)
                ? await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id) : null;

            // Solo se puede elegir una obra social con convenio vigente (o conservar la que ya tenía).
            if (paciente != null && datos.ObraSocialId is { } osId && osId != paciente.ObraSocialId
                && !await _context.ObrasSociales.AnyAsync(o => o.Id == osId && o.Activa))
                ModelState.AddModelError("Datos.ObraSocialId", "La obra social elegida no es válida.");

            if (!ModelState.IsValid)
                return View(nameof(Index), await ConstruirAsync(datos));

            usuario.PhoneNumber = Limpiar(datos.Telefono);
            if (paciente != null)
            {
                paciente.Direccion = Limpiar(datos.Direccion);
                paciente.ObraSocialId = datos.ObraSocialId;
                paciente.NumeroAfiliado = Limpiar(datos.NumeroAfiliado);
                paciente.Alergias = Limpiar(datos.Alergias);
            }

            // UpdateAsync también guarda los cambios pendientes del perfil de paciente (mismo DbContext).
            var resultado = await _userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(nameof(Index), await ConstruirAsync(datos));
            }

            TempData["Ok"] = "Tus datos se guardaron correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarClave([Bind(Prefix = "Clave")] CambiarClaveViewModel clave)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            if (ModelState.IsValid)
            {
                var resultado = await _userManager.ChangePasswordAsync(usuario, clave.ClaveActual, clave.ClaveNueva);
                if (resultado.Succeeded)
                {
                    // Cambiar la contraseña invalida la sesión; se renueva para que no tenga que volver a ingresar.
                    await _signInManager.RefreshSignInAsync(usuario);
                    TempData["Ok"] = "Tu contraseña se cambió correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in resultado.Errors)
                {
                    var campo = error.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? "Clave.ClaveActual"
                              : error.Code.StartsWith("Password") ? "Clave.ClaveNueva" : string.Empty;
                    ModelState.AddModelError(campo, error.Description);
                }
            }

            // Nunca se devuelven las contraseñas escritas al formulario.
            return View(nameof(Index), await ConstruirAsync(null, new CambiarClaveViewModel()));
        }

        private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private async Task<PerfilViewModel?> ConstruirAsync(PerfilFormViewModel? datos = null, CambiarClaveViewModel? clave = null)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return null;

            var vm = new PerfilViewModel
            {
                NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}",
                Email = usuario.Email ?? "",
                Dni = usuario.Dni,
                EsPaciente = User.IsInRole(Roles.Paciente),
                EsOdontologo = User.IsInRole(Roles.Odontologo),
                Rol = User.IsInRole(Roles.Administrador) ? "Administrador" : User.IsInRole(Roles.Odontologo) ? "Odontólogo/a" : "Paciente",
                Datos = new PerfilFormViewModel { Telefono = usuario.PhoneNumber }
            };

            if (vm.EsPaciente)
            {
                var p = await _context.Pacientes.AsNoTracking().Include(x => x.ObraSocial)
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id);
                if (p != null)
                {
                    vm.FechaNacimiento = p.FechaNacimiento;
                    vm.ObraSocial = p.ObraSocial?.Nombre;
                    vm.CoberturaObraSocial = p.ObraSocial?.Cobertura;
                    vm.Datos.Direccion = p.Direccion;
                    vm.Datos.ObraSocialId = p.ObraSocialId;
                    vm.Datos.NumeroAfiliado = p.NumeroAfiliado;
                    vm.Datos.Alergias = p.Alergias;
                }

                // Obras sociales con convenio vigente, más la actual aunque ya no lo tenga.
                var actual = vm.Datos.ObraSocialId;
                ViewBag.ObrasSociales = await _context.ObrasSociales.AsNoTracking()
                    .Where(o => o.Activa || o.Id == actual)
                    .OrderBy(o => o.Nombre)
                    .Select(o => new SelectListItem(o.Nombre, o.Id.ToString())).ToListAsync();
            }

            if (vm.EsOdontologo)
            {
                var o = await _context.Odontologos.AsNoTracking().Include(x => x.Especialidad).Include(x => x.Horarios)
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id);
                if (o != null)
                {
                    vm.Matricula = o.Matricula;
                    vm.Especialidad = o.Especialidad?.Nombre;
                    vm.Horarios = o.Horarios.ToList();
                }
            }

            // Si el formulario volvió con errores, se conserva lo que la persona había escrito.
            if (datos != null) vm.Datos = datos;
            if (clave != null) vm.Clave = clave;
            return vm;
        }
    }
}
