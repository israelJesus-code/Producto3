using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Recaptcha;

namespace SistemaLegalPagares.Areas.Identity.Pages.Account;

[EnableRateLimiting("login")]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRecaptchaService _recaptcha;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IRecaptchaService recaptcha,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _recaptcha = recaptcha;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string RecaptchaSiteKey => _recaptcha.SiteKey;

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
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
        }

        if (ModelState.IsValid && recaptchaOk)
        {
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                NombreCompleto = Input.NombreCompleto,
                EstaAprobado = false,
                EmailConfirmed = true,
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Nuevo abogado registrado, pendiente de aprobación: {Email}", user.Email);
                await _userManager.AddToRoleAsync(user, DbInitializer.RolAbogado);

                TempData["RegisterSuccess"] =
                    "Tu solicitud fue enviada. Un administrador debe aprobar tu cuenta antes de que puedas iniciar sesión.";
                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }
}
