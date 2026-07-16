namespace SistemaLegalPagares.Services.Recaptcha;

public interface IRecaptchaService
{
    string SiteKey { get; }

    /// <summary>Valida un token de Google reCAPTCHA contra la API siteverify.</summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
}
