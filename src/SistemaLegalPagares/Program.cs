using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Middleware;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Email;
using SistemaLegalPagares.Services.Recaptcha;
using SistemaLegalPagares.Services.Security;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- Identity (autenticación por cookies para la UI MVC) ----------
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // Un usuario autenticado con rol insuficiente recibe 403 en vez de una redirección,
    // conforme al esquema de pruebas de seguridad (rol insuficiente -> 403 Forbidden).
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// ---------- JWT (autenticación de la Web API propia, independiente de las cookies) ----------
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "clave-de-desarrollo-cambia-esto-por-favor-32chars";
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddScoped<JwtTokenService>();

// ---------- Servicios de terceros ----------
builder.Services.AddHttpClient<IRecaptchaService, GoogleRecaptchaService>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IAppEmailSender, SmtpEmailSender>();

// ---------- Rate limiting (mitiga fuerza bruta en login) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueLimit = 0,
            }));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sistema Legal de Pagarés API",
        Version = "v1",
        Description = "Web service propio (REST + JWT) para clientes, expedientes, deudores y pagarés.",
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Token JWT obtenido en POST /api/v1/auth/login",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

// ---------- Migraciones automáticas + seed (con reintentos: SQL Server puede tardar en iniciar) ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const int maxRetries = 10;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DbInitializer.SeedRolesAndAdminAsync(scope.ServiceProvider, app.Configuration);
            logger.LogInformation("Migraciones aplicadas y seed inicial completado.");
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Intento {Attempt}/{Max} fallido esperando la base de datos, reintentando...", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// ---------- Pipeline HTTP ----------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Legal de Pagarés API v1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Legal de Pagarés API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<EstaAprobadoMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Necesario para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program { }
