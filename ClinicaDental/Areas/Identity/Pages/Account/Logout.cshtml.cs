#nullable disable

using ClinicaDental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClinicaDental.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;

        public LogoutModel(SignInManager<Usuario> signInManager) => _signInManager = signInManager;

        // Si alguien entra a /Identity/Account/Logout escribiendo la dirección, simplemente se lo lleva al inicio.
        public IActionResult OnGet() => RedirectToAction("Index", "Home", new { area = "" });

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return returnUrl != null ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
