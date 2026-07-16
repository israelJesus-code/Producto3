using Microsoft.AspNetCore.Identity;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Data;

public static class DbInitializer
{
    public const string RolAdmin = "Admin";
    public const string RolAbogado = "Abogado";

    public static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var rol in new[] { RolAdmin, RolAbogado })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var adminEmail = config["AdminSeed:Email"] ?? "admin@legal.com";
        var adminPassword = config["AdminSeed:Password"] ?? "Admin123*";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador del Sistema",
                EmailConfirmed = true,
                EstaAprobado = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, RolAdmin);
            }
        }
        else if (!await userManager.IsInRoleAsync(admin, RolAdmin))
        {
            await userManager.AddToRoleAsync(admin, RolAdmin);
        }
    }
}
