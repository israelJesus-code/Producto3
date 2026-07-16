# Sistema Legal de Pagarés — Producto 3

Universidad Tecnológica de Puebla · Desarrollo Web Integral
Alumno: Israel Jesús García Osorio · Profesor: Javier Nolasco Hernández

Aplicación web para la gestión de pagarés conforme al Artículo 170 de la Ley General de Títulos y
Operaciones de Crédito (LGTOC), construida como continuación del caso de estudio del Producto 1
(Sistema Legal de Pagarés), manteniendo su misma arquitectura MVC, patrones de diseño, frameworks
(ASP.NET Core 8, EF Core, Identity, QuestPDF) y esquema de pruebas, e incorporando:

1. **Mecanismos de seguridad**: ASP.NET Core Identity (hash PBKDF2), flujo de aprobación de abogados
   (`EstaAprobado`), roles Admin/Abogado, CSRF (antiforgery), rate limiting en login/registro, cabeceras
   de seguridad HTTP, JWT para la API, 403 por rol insuficiente, aislamiento de datos por abogado.
2. **Web Services de terceros**: Google reCAPTCHA v2 (verificación server-side) en login/registro, y un
   servicio de correo (SMTP) para notificar aprobación/rechazo de abogados.
3. **Web Services propios**: API REST (`/api/v1/...`) documentada con Swagger, autenticada con JWT,
   independiente de las cookies de la UI MVC.
4. **Repositorio en funcionamiento**: ver sección "Cómo ejecutar" abajo (Docker, un solo comando).

## Stack

ASP.NET Core 8 MVC + Web API · Entity Framework Core 8 · SQL Server 2022 · ASP.NET Core Identity ·
QuestPDF (Community) · JWT Bearer · Swagger/OpenAPI · Docker / Docker Compose.

## Cómo ejecutar (Docker)

```bash
cp .env.example .env    # completa los valores (o usa los de prueba incluidos)
docker compose up --build
```

- App web: http://localhost:8080
- Swagger (API propia): http://localhost:8080/swagger
- Admin sembrado automáticamente: `admin@legal.com` / `Admin123*` (configurable vía `AdminSeed:Email` / `AdminSeed:Password`)

Las migraciones de EF Core se aplican automáticamente al iniciar el contenedor `web`.

## Estructura del repositorio

```
src/SistemaLegalPagares/     # Proyecto principal (MVC + API)
  Controllers/                # HomeController, Clientes, Deudores, Expedientes, Pagares, Admin
  Controllers/Api/             # AuthApiController + API REST propia (JWT)
  Areas/Identity/Pages/Account/ # Login, Register (con reCAPTCHA), Logout, AccessDenied
  Data/                        # ApplicationDbContext, DbInitializer, Migrations
  Models/                      # ApplicationUser, Cliente, Expediente, Pagare, Deudor, SubPagare, PagareDeudor
  Middleware/                  # EstaAprobadoMiddleware, SecurityHeadersMiddleware
  Services/Pdf/                 # PagarePdfDocument (QuestPDF, formato Formitec)
  Services/Recaptcha/           # GoogleRecaptchaService
  Services/Email/                # SmtpEmailSender
  Services/Security/             # JwtTokenService
tests/SistemaLegalPagares.Tests/ # xUnit + Moq (unitarias) sobre el esquema de pruebas del Producto 1
Dockerfile, docker-compose.yml   # Despliegue local
```

## Referencia

Producto 1 (caso de estudio base): `P1_PAGARES.docx.pdf` (no incluido en este repositorio por ser
trabajo de otro equipo; usado solo como referencia de arquitectura).
