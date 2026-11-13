using ClinicaDental.Models;
using Microsoft.AspNetCore.Identity;

namespace ClinicaDental.Data
{
    public static class Inicializador
    {
        public const string AdminEmail = "admin@clinica.com";
        public const string AdminClave = "Admin123!";

        public static async Task CrearRolesYAdministradorAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<Usuario>>();

            foreach (var rol in Roles.Todos)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            var admin = await userManager.FindByEmailAsync(AdminEmail);
            if (admin == null)
            {
                admin = new Usuario
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    Nombre = "Administración",
                    Apellido = "Clínica",
                    Dni = 11111111,
                    Activo = true
                };

                var resultado = await userManager.CreateAsync(admin, AdminClave);
                if (!resultado.Succeeded)
                    throw new InvalidOperationException("No se pudo crear el administrador: " +
                        string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(admin, Roles.Administrador))
                await userManager.AddToRoleAsync(admin, Roles.Administrador);
        }
    }
}
