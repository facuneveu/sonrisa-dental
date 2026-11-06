#nullable disable

using System.ComponentModel.DataAnnotations;
using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Areas.Identity.Pages.Account
{
    // Autoregistro: cualquier persona puede crear su cuenta, y siempre queda como Paciente.
    // Los odontólogos y administradores los da de alta la administración.
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager,
            ApplicationDbContext context, ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public List<SelectListItem> ObrasSociales { get; set; } = new();

        public DateOnly Hoy => Reloj.Hoy;

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [StringLength(50, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El apellido es obligatorio.")]
            [StringLength(50, ErrorMessage = "El apellido no puede superar los {1} caracteres.")]
            [Display(Name = "Apellido")]
            public string Apellido { get; set; }

            [Required(ErrorMessage = "El DNI es obligatorio.")]
            [Range(1000000, 99999999, ErrorMessage = "Ingresá un DNI válido (7 u 8 dígitos).")]
            [Display(Name = "DNI")]
            public int? Dni { get; set; }

            [Required(ErrorMessage = "El teléfono es obligatorio.")]
            [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
            [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
            [Display(Name = "Teléfono")]
            public string Telefono { get; set; }

            [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
            [Display(Name = "Fecha de nacimiento")]
            public DateOnly? FechaNacimiento { get; set; }

            [Display(Name = "Obra social")]
            public int? ObraSocialId { get; set; }

            [StringLength(30, ErrorMessage = "El número de afiliado no puede superar los {1} caracteres.")]
            [Display(Name = "Nº de afiliado")]
            public string NumeroAfiliado { get; set; }

            [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
            [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [StringLength(100, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        private async Task CargarObrasSocialesAsync()
        {
            ObrasSociales = await _context.ObrasSociales.Where(o => o.Activa).OrderBy(o => o.Nombre)
                .Select(o => new SelectListItem(o.Nombre, o.Id.ToString())).ToListAsync();
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            await CargarObrasSocialesAsync();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            if (Input.FechaNacimiento is { } nac && (nac > Reloj.Hoy || nac.Year < 1900))
                ModelState.AddModelError("Input.FechaNacimiento", "Ingresá una fecha de nacimiento válida.");

            if (Input.Dni is { } dni && await _context.Users.AnyAsync(u => u.Dni == dni))
                ModelState.AddModelError("Input.Dni", "Ya existe una cuenta registrada con ese DNI.");

            if (Input.ObraSocialId is { } osId && !await _context.ObrasSociales.AnyAsync(o => o.Id == osId && o.Activa))
                ModelState.AddModelError("Input.ObraSocialId", "La obra social elegida no es válida.");

            if (ModelState.IsValid)
            {
                var usuario = new Usuario
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    PhoneNumber = Input.Telefono,
                    Nombre = Input.Nombre.Trim(),
                    Apellido = Input.Apellido.Trim(),
                    Dni = Input.Dni!.Value,
                    Activo = true
                };

                // La cuenta y el perfil de paciente se crean juntos o no se crea ninguno.
                await using var transaccion = await _context.Database.BeginTransactionAsync();

                var resultado = await _userManager.CreateAsync(usuario, Input.Password);
                if (resultado.Succeeded)
                {
                    await _userManager.AddToRoleAsync(usuario, Roles.Paciente);

                    _context.Pacientes.Add(new Paciente
                    {
                        UsuarioId = usuario.Id,
                        FechaNacimiento = Input.FechaNacimiento!.Value,
                        ObraSocialId = Input.ObraSocialId,
                        NumeroAfiliado = string.IsNullOrWhiteSpace(Input.NumeroAfiliado) ? null : Input.NumeroAfiliado.Trim()
                    });
                    await _context.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    _logger.LogInformation("Se registró un nuevo paciente: {Email}.", Input.Email);
                    await _signInManager.SignInAsync(usuario, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            await CargarObrasSocialesAsync();
            return Page();
        }
    }
}
