namespace SistemaLegalPagares.Services.Email;

public interface IAppEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken ct = default);
}
