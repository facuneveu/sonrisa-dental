#nullable disable

using System.ComponentModel.DataAnnotations;
using ClinicaDental.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClinicaDental.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _config;

        public LoginModel(SignInManager<Usuario> signInManager, UserManager<Usuario> userManager,
            ILogger<LoginModel> logger, IConfiguration config)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _config = config;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        // Las cuentas de prueba solo se muestran mientras la base tenga datos de ejemplo.
        public bool MostrarCuentasDemo => _config.GetValue("SeedDatosEjemplo", true);

        public class InputModel
        {
            [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
            [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Ingresá tu contraseña.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Display(Name = "Mantener la sesión iniciada")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid) return Page();

            // Solo se avisa que la cuenta está desactivada si la contraseña es correcta; si no, cualquiera podría
            // averiguar qué correos están registrados probando direcciones.
            var usuario = await _userManager.FindByEmailAsync(Input.Email);
            if (usuario != null && !usuario.Activo && await _userManager.CheckPasswordAsync(usuario, Input.Password))
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Comunicate con la clínica.");
                return Page();
            }

            var resultado = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            if (resultado.Succeeded)
            {
                _logger.LogInformation("Inició sesión {Email}.", Input.Email);
                return LocalRedirect(returnUrl);
            }

            if (resultado.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Demasiados intentos fallidos. Esperá unos minutos antes de volver a intentar.");
            else
                ModelState.AddModelError(string.Empty, "El correo o la contraseña son incorrectos.");

            return Page();
        }
    }
}
