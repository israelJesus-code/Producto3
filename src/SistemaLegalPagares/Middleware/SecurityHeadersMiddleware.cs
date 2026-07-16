namespace SistemaLegalPagares.Middleware;

/// <summary>Agrega cabeceras HTTP de seguridad a toda respuesta.</summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://www.google.com https://www.gstatic.com; " +
                "frame-src https://www.google.com; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:;";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
