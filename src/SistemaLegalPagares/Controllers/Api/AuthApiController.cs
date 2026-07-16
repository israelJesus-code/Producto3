using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Security;

namespace SistemaLegalPagares.Controllers.Api;

/// <summary>Punto de entrada de la Web API propia: emite un JWT independiente de las cookies de la UI MVC.</summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>Autentica un abogado/admin aprobado y devuelve un token JWT válido por 4 horas.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        if (!user.EstaAprobado)
        {
            return Unauthorized(new { message = "La cuenta aún no ha sido aprobada por un administrador." });
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwtTokenService.GenerateToken(user, roles);

        return Ok(new LoginResponse(token, expires, user.NombreCompleto, roles));
    }
}
