using Microsoft.AspNetCore.Identity;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Middleware;

/// <summary>
/// Verifica en cada petición autenticada que el usuario tenga EstaAprobado = true.
/// Si un abogado fue registrado pero aún no ha sido aprobado por un Admin, se cierra
/// su sesión y se le redirige al login con blocked=true.
/// </summary>
public class EstaAprobadoMiddleware
{
    private readonly RequestDelegate _next;

    public EstaAprobadoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SignInManager<ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.Request.Path.StartsWithSegments("/Identity")
            && !context.Request.Path.StartsWithSegments("/api"))
        {
            var user = await signInManager.UserManager.GetUserAsync(context.User);
            if (user is not null && !user.EstaAprobado)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Identity/Account/Login?blocked=true");
                return;
            }
        }

        await _next(context);
    }
}
