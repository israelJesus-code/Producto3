using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SistemaLegalPagares.Services.Email;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "SistemaLegalPagares <no-reply@sistemalegalpagares.local>";
}

/// <summary>
/// Envía correos de notificación (aprobación/rechazo de abogados) vía SMTP.
/// Si no hay credenciales SMTP configuradas, registra el correo en el log
/// en lugar de fallar la operación de negocio que lo dispara.
/// </summary>
public class SmtpEmailSender : IAppEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP no configurado; correo simulado para {ToEmail}: {Subject}", toEmail, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlMessage };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
            if (!string.IsNullOrWhiteSpace(_options.User))
            {
                await client.AuthenticateAsync(_options.User, _options.Password, ct);
            }
            await client.SendAsync(message, ct);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}
