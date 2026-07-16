using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Email;

namespace SistemaLegalPagares.Controllers;

[Authorize(Roles = DbInitializer.RolAdmin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppEmailSender _emailSender;
    private readonly ILogger<AdminController> _logger;

    public AdminController(UserManager<ApplicationUser> userManager, IAppEmailSender emailSender, ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<IActionResult> UsuariosPendientes()
    {
        var todos = _userManager.Users.Where(u => !u.EstaAprobado).ToList();
        var pendientes = new List<ApplicationUser>();
        foreach (var user in todos)
        {
            if (!await _userManager.IsInRoleAsync(user, DbInitializer.RolAdmin))
            {
                pendientes.Add(user);
            }
        }

        return View(pendientes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.EstaAprobado = true;
        await _userManager.UpdateAsync(user);

        if (!await _userManager.IsInRoleAsync(user, DbInitializer.RolAbogado))
        {
            await _userManager.AddToRoleAsync(user, DbInitializer.RolAbogado);
        }

        _logger.LogInformation("Usuario aprobado: {Email}", user.Email);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _emailSender.SendEmailAsync(
                user.Email,
                "Tu cuenta fue aprobada - Sistema Legal de Pagarés",
                $"<p>Hola {user.NombreCompleto},</p><p>Tu cuenta de abogado ha sido <strong>aprobada</strong>. Ya puedes iniciar sesión en el sistema.</p>");
        }

        TempData["Mensaje"] = $"Usuario {user.Email} aprobado correctamente.";
        return RedirectToAction(nameof(UsuariosPendientes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var email = user.Email;
        var nombre = user.NombreCompleto;
        await _userManager.DeleteAsync(user);
        _logger.LogInformation("Usuario rechazado y eliminado: {Email}", email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _emailSender.SendEmailAsync(
                email,
                "Solicitud de acceso rechazada - Sistema Legal de Pagarés",
                $"<p>Hola {nombre},</p><p>Tu solicitud de acceso al Sistema Legal de Pagarés fue rechazada por el administrador.</p>");
        }

        TempData["Mensaje"] = $"Usuario {email} rechazado y eliminado.";
        return RedirectToAction(nameof(UsuariosPendientes));
    }
}
