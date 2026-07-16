using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Recaptcha;

namespace SistemaLegalPagares.Areas.Identity.Pages.Account;

[EnableRateLimiting("login")]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRecaptchaService _recaptcha;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IRecaptchaService recaptcha,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _recaptcha = recaptcha;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string RecaptchaSiteKey => _recaptcha.SiteKey;

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool Blocked { get; set; }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null, bool blocked = false)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        Blocked = blocked;
        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
        var recaptchaOk = await _recaptcha.VerifyAsync(recaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());
        if (!recaptchaOk)
        {
            ModelState.AddModelError(string.Empty, "Verificación reCAPTCHA fallida. Intenta de nuevo.");
            return Page();
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user is not null && !user.EstaAprobado)
            {
                ModelState.AddModelError(string.Empty,
                    "Tu cuenta aún no ha sido aprobada por un administrador.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario autenticado: {Email}", Input.Email);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente por múltiples intentos fallidos.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
        }

        return Page();
    }
}
