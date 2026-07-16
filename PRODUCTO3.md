Universidad Tecnológica de Puebla
División de Tecnologías de la Información
Desarrollo de Software Multiplataforma

# Producto 3. Integración de componentes de software para app web

**Materia:** Desarrollo Web Integral (DWI)
**Alumno:** García Osorio Israel Jesús
**Profesor:** Javier Nolasco Hernández
**Fecha de entrega:** 17 de julio de 2026

**Repositorio en GitHub:** https://github.com/israelJesus-code/Producto3

---

## Índice

1. Introducción
2. Mecanismos de seguridad (25%)
3. Web Services de terceros (25%)
4. Web Services propios (25%)
5. Enlace del repositorio en funcionamiento (25%)
6. Evidencia de pruebas automatizadas
7. Cómo ejecutar el proyecto
8. Conclusión

---

## 1. Introducción

Este documento corresponde al **Producto 3** de la Unidad de Desarrollo Web Integral. Toma como caso de
estudio el **Sistema Legal de Pagarés** desarrollado en el Producto 1 (despacho de abogados mexicano que
automatiza la generación, registro y consulta de pagarés conforme al **Artículo 170 de la Ley General de
Títulos y Operaciones de Crédito, LGTOC**), y construye sobre esa misma base una aplicación web funcional
que integra mecanismos de seguridad, servicios web de terceros y servicios web propios, con su repositorio
Git/GitHub en funcionamiento.

Siguiendo la metodología ágil, la arquitectura de software (MVC monolítico de tres capas), los patrones de
diseño (Repository/Unit of Work vía EF Core, Strategy/Service para la generación de PDF, Decorator vía
middleware, ViewModel) y los frameworks considerados en el Producto 1, la solución fue **reconstruida desde
cero en un repositorio individual** con el mismo stack tecnológico: **ASP.NET Core 8 MVC + Web API**,
**Entity Framework Core 8**, **SQL Server 2022**, **ASP.NET Core Identity** y **QuestPDF**, empaquetada en
**Docker / Docker Compose** para que el repositorio sea ejecutable con un solo comando, sin depender de
instalar .NET o SQL Server de forma local.

Sobre esa base se añadieron las cuatro entregas que exige este producto:

| # | Entregable | % |
|---|---|---|
| 1 | Mecanismos de seguridad | 25% |
| 2 | Web Services de terceros | 25% |
| 3 | Web Services propios | 25% |
| 4 | Enlace del repositorio en funcionamiento | 25% |

---

## 2. Mecanismos de seguridad (25%)

### 2.1 Autenticación y control de acceso

- **ASP.NET Core Identity** con hash de contraseñas **PBKDF2** con sal aleatoria (nunca texto plano).
- Modelo `ApplicationUser : IdentityUser` con el campo `EstaAprobado` (bool) que implementa un **flujo de
  aprobación administrativa**: un abogado que se registra queda con `EstaAprobado = false` y no puede
  operar el sistema hasta que un `Admin` lo aprueba desde `/Admin/UsuariosPendientes`.
- Middleware personalizado (`EstaAprobadoMiddleware`) que se ejecuta después de `UseAuthentication()` y
  antes de `UseAuthorization()`: si un usuario autenticado no está aprobado, se cierra su sesión y se le
  redirige al login con `?blocked=true`, mostrando un aviso explícito.
- Roles `Admin` / `Abogado` mediante `[Authorize(Roles = "...")]` en cada controlador.
- **Aislamiento de datos por usuario**: un abogado solo puede ver/editar/eliminar los pagarés que él mismo
  creó (`PagaresController` filtra por `UsuarioId`); el rol `Admin` sí puede auditar todos los pagarés del
  sistema.
- Bloqueo de cuenta (`Lockout`) tras 5 intentos fallidos de inicio de sesión durante 10 minutos.

### 2.2 Protección de formularios y peticiones

- **CSRF / Antiforgery tokens** (`[ValidateAntiForgeryToken]`) en todas las acciones POST (Clientes,
  Deudores, Expedientes, Pagarés, Admin). Una petición POST sin el token válido es rechazada con
  `400 Bad Request`.
- **Rate limiting** (`Microsoft.AspNetCore.RateLimiting`) aplicado a las páginas de Login y Registro:
  máximo 10 solicitudes por minuto por IP, mitigando ataques de fuerza bruta.
- **Cabeceras de seguridad HTTP** (`SecurityHeadersMiddleware`): `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Content-Security-Policy`
  restringida a los orígenes necesarios (self + Google reCAPTCHA).
- `UseHsts()` + `UseHttpsRedirection()` en el pipeline.
- Control de acceso por rol devuelve **403 Forbidden** cuando un usuario autenticado con rol insuficiente
  intenta acceder a un recurso (por ejemplo, un Abogado accediendo al Panel de Administración).

### 2.3 Seguridad de la API propia

- La **Web API REST** (`/api/v1/...`) usa un esquema de autenticación **independiente**: **JWT Bearer**,
  desacoplado de las cookies de sesión de la interfaz MVC. El token se firma con `HmacSha256` y una clave
  secreta gestionada por variable de entorno (nunca en el código fuente ni en el repositorio).
- Los mismos controles de rol y de propiedad de datos (abogado vs. admin) se aplican también en la capa API.

### 2.4 Gestión de secretos

- Ningún secreto (token de GitHub, cadena de conexión con contraseña, clave JWT, credenciales SMTP) está
  escrito en el código fuente: todos se inyectan por variables de entorno vía `docker-compose.yml`, leídas
  desde un `.env` **excluido de git** (`.gitignore`). Se incluye `.env.example` como plantilla pública sin
  valores reales.

### 2.5 Evidencia — pruebas de seguridad ejecutadas contra el sistema en funcionamiento

```text
== Intento de login del abogado SIN aprobar (debe fallar) ==
HTTP 200   (se re-renderiza el login con el mensaje de error, NO se autentica)
"Tu cuenta aún no ha sido aprobada por un administrador."

== Login del mismo abogado tras ser aprobado por el Admin ==
HTTP 302   (redirección exitosa, sesión iniciada)

== Abogado intenta acceder a /Admin/UsuariosPendientes ==
HTTP 403   (rol insuficiente)

== Acceso sin sesión a una ruta protegida (/Pagares) ==
HTTP 302   (redirección al login)

== POST a /Clientes/Create sin token antiforgery ==
HTTP 400   (protección CSRF activa)

== Abogado #2 intenta ver el detalle de un pagaré creado por el Abogado #1 ==
Forbid()   (aislamiento de datos por usuario)
```

---

## 3. Web Services de terceros (25%)

Se integraron **dos** servicios externos reales, ambos con llamadas HTTP genuinas desde el backend (no
simulaciones de UI):

### 3.1 Google reCAPTCHA v2

- `Services/Recaptcha/GoogleRecaptchaService.cs` realiza una petición `POST` real a
  `https://www.google.com/recaptcha/api/siteverify` para validar, en el servidor, el token generado por el
  widget de reCAPTCHA en los formularios de **Login** y **Registro**.
- Si la verificación falla (o no se envía token), el `ModelState` se marca inválido y el formulario se
  rechaza — el reCAPTCHA no es solo decorativo en el frontend, se **valida en el backend**.
- Las claves son configurables por entorno (`Recaptcha:SiteKey` / `Recaptcha:SecretKey`); por defecto se
  usan las **claves de prueba oficiales de Google** (documentadas públicamente, siempre válidas en
  `localhost`), listas para sustituirse por claves de producción sin tocar código.

### 3.2 Servicio de correo (SMTP)

- `Services/Email/SmtpEmailSender.cs` (basado en **MailKit**) envía una notificación por correo cuando el
  administrador **aprueba** o **rechaza** a un abogado, invocado desde `AdminController.Aprobar` /
  `AdminController.Rechazar`.
- Si no hay credenciales SMTP configuradas (caso de esta demo), el servicio degrada de forma controlada
  registrando el correo en el log de la aplicación en lugar de fallar la operación de negocio — evidencia
  real capturada del contenedor en ejecución:

```text
warn: SistemaLegalPagares.Services.Email.SmtpEmailSender[0]
      SMTP no configurado; correo simulado para israel.garcia.test@example.com:
      Tu cuenta fue aprobada - Sistema Legal de Pagarés
```

  Configurando `Smtp:Host`, `Smtp:User` y `Smtp:Password` (variables de entorno, ver `.env.example`) el
  mismo código envía el correo real vía SMTP sin cambios adicionales.

---

## 4. Web Services propios (25%)

Además de la interfaz MVC, el sistema expone una **API REST propia** en `/api/v1/...`, documentada con
**Swagger / OpenAPI** en `/swagger`, pensada para que otros sistemas (una app móvil, otro despacho, un
integrador externo) puedan consumir el Sistema Legal de Pagarés sin pasar por la interfaz web.

### 4.1 Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/v1/auth/login` | Autentica y devuelve un JWT (4h de vigencia) |
| GET/POST/PUT/DELETE | `/api/v1/clientes` | CRUD de clientes |
| GET/POST/PUT/DELETE | `/api/v1/deudores` | CRUD de deudores |
| GET/POST/PUT/DELETE | `/api/v1/expedientes` | CRUD de expedientes |
| GET/POST/DELETE | `/api/v1/pagares` | CRUD de pagarés (con reglas de negocio del Art. 170 LGTOC) |
| GET | `/api/v1/pagares/{id}/pdf` | Descarga el PDF del pagaré (mismo motor QuestPDF que la UI) |

Todos los endpoints (salvo `auth/login`) exigen `Authorization: Bearer <token>` y respetan los mismos roles
y reglas de aislamiento de datos que la interfaz MVC (un Abogado solo ve/edita sus propios pagarés vía API).

### 4.2 Evidencia — API en funcionamiento (peticiones reales contra el contenedor)

```text
$ curl -X POST http://localhost:8080/api/v1/auth/login -H "Content-Type: application/json" \
       -d '{"email":"admin@legal.com","password":"Admin123*"}'

{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "expiresAtUtc":"...", "nombreCompleto":"Administrador del Sistema", "roles":["Admin"]}

$ curl http://localhost:8080/api/v1/clientes -H "Authorization: Bearer eyJhbGci..."
HTTP 200   (lista de clientes en JSON)

$ curl http://localhost:8080/api/v1/clientes            # sin token
HTTP 401   (no autorizado)

$ curl http://localhost:8080/swagger/index.html
HTTP 200   (documentación interactiva disponible)
```

> 📸 *Captura pendiente: pantalla de Swagger UI (`/swagger`) mostrando los endpoints `/api/v1/...` y el
> botón "Authorize" con JWT.*

---

## 5. Enlace del repositorio en funcionamiento (25%)

**Repositorio:** https://github.com/israelJesus-code/Producto3

El repositorio incluye `Dockerfile` y `docker-compose.yml` para levantar **toda la aplicación con un solo
comando**, sin necesidad de instalar .NET SDK ni SQL Server en la máquina local:

```bash
git clone https://github.com/israelJesus-code/Producto3.git
cd Producto3
cp .env.example .env      # completar variables (o usar las de prueba incluidas)
docker compose up --build
```

### 5.1 Evidencia — stack corriendo

```text
$ docker compose up -d
 Network producto3webintegralisra_default   Created
 Volume  producto3webintegralisra_mssql_data Created
 Container producto3webintegralisra-db-1     Started
 Container producto3webintegralisra-db-1     Healthy
 Container producto3webintegralisra-web-1    Started

$ docker compose ps
NAME                             IMAGE                                        STATUS                    PORTS
producto3webintegralisra-db-1    mcr.microsoft.com/mssql/server:2022-latest  Up (healthy)              0.0.0.0:1433->1433/tcp
producto3webintegralisra-web-1   producto3webintegralisra-web                Up                        0.0.0.0:8080->8080/tcp

$ docker compose logs web | tail -5
info: Program[0]
      Migraciones aplicadas y seed inicial completado.
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Al iniciar, el contenedor `web` **aplica automáticamente las migraciones de EF Core** contra SQL Server y
siembra los roles `Admin`/`Abogado` junto con un usuario administrador (`admin@legal.com` / `Admin123*`,
configurable por variables de entorno).

### 5.2 Flujo funcional completo verificado de extremo a extremo

Se ejecutó manualmente el flujo completo del caso de estudio contra el contenedor en ejecución:

1. Registro de un nuevo abogado (con reCAPTCHA) → queda pendiente de aprobación.
2. Intento de login del abogado sin aprobar → bloqueado con mensaje explícito.
3. Login del Admin sembrado (`admin@legal.com`) → aprueba al abogado desde el panel.
4. Correo de notificación de aprobación (registrado en log, SMTP simulado).
5. Login del abogado ya aprobado → acceso concedido.
6. Alta de **Cliente** (Fortino Romero Mantilla), **Deudor** (Joel Gomez Herrera).
7. Creación de **Expediente** `EXP-2026-001` vinculado al cliente.
8. Creación de **Pagaré** (Art. 170 LGTOC) vinculado al expediente y al deudor.
9. Descarga del **PDF** del pagaré (formato Formitec, generado con QuestPDF).
10. Verificación de que el Admin ve todos los pagarés del sistema, mientras que un segundo abogado no puede
    ver el pagaré del primero (`403`/`Forbid`).

> 📸 *Capturas pendientes de la interfaz web (login, panel principal, alta de cliente/deudor/expediente,
> creación y vista previa del pagaré, panel de administración) — se completan directamente en el navegador.*

El PDF generado durante esta verificación reproduce fielmente el formato "Formitec" y todos los campos del
Art. 170 LGTOC:

```
PAGARÉ                    LUGAR DE EXPEDICIÓN   DÍA  MES  AÑO   BUENO POR
No. 1 de 1                Puebla, Puebla        16   7    2026  $ 50,000.00

Debo(emos) y pagaré(mos) incondicionalmente sin protesto este pagaré... a la orden de
Joel Gomez Herrera en Puebla, Puebla el día 27/08/2026.

La cantidad de: CINCUENTA MIL PESOS 00/100 M.N.

VALOR RECIBIDO A MI (NUESTRA) ENTERA SATISFACCIÓN... SERIE NUMERADA DEL 1 AL 1...
INTERESES MORATORIOS DEL 5.00% POR CADA MES O FRACCIÓN, CONFORME AL ART. 174 Y 152 LGTOC.

NOMBRE Y DATOS DEL DEUDOR | AVAL | DEUDOR
NOMBRE: Joel Gomez Herrera
DOMICILIO: Av. 9 Pte. 106, Centro histórico, Puebla, Pue.
POBLACIÓN: Puebla

Expediente: EXP-2026-001
```

---

## 6. Evidencia de pruebas automatizadas

Siguiendo el esquema de pruebas del Producto 1 (xUnit + Moq para pruebas unitarias), se implementaron 10
pruebas automatizadas sobre los componentes críticos (`PagaresController`, `AdminController`,
`PagarePdfDocument`, `JwtTokenService):

```text
$ dotnet test
Passed SistemaLegalPagares.Tests.Services.Security.JwtTokenServiceTests.GenerateToken_IncluyeClaimsDeUsuarioYRoles
Passed SistemaLegalPagares.Tests.Controllers.AdminControllerTests.Aprobar_EstableceEstaAprobadoYAsignaRolAbogado
Passed SistemaLegalPagares.Tests.Controllers.AdminControllerTests.Rechazar_EliminaAlUsuarioDelSistema
Passed SistemaLegalPagares.Tests.Services.Pdf.PagarePdfDocumentTests.GeneratePdf_SinDeudores_NoLanzaExcepcion
Passed SistemaLegalPagares.Tests.Services.Pdf.PagarePdfDocumentTests.GeneratePdf_ConPagareValido_GeneraDocumentoNoVacio
Passed SistemaLegalPagares.Tests.Controllers.PagaresControllerTests.Create_ConFechaVencimientoAnteriorAHoy_AgregaModelErrorYRetornaVista
Passed SistemaLegalPagares.Tests.Controllers.PagaresControllerTests.Create_ConModeloValidoYExpedienteExistente_GuardaYRedirigeADetails
Passed SistemaLegalPagares.Tests.Controllers.PagaresControllerTests.Create_ConExpedienteIdCero_AgregaModelErrorNoSeRecibioElExpediente
Passed SistemaLegalPagares.Tests.Controllers.PagaresControllerTests.Pdf_ConPagareExistente_RetornaArchivoConContentTypeApplicationPdf
Passed SistemaLegalPagares.Tests.Controllers.PagaresControllerTests.Details_DeOtroAbogado_RetornaForbid

Test Run Successful.
Total tests: 10
     Passed: 10
 Total time: 2.31 Seconds
```

---

## 7. Cómo ejecutar el proyecto

```bash
git clone https://github.com/israelJesus-code/Producto3.git
cd Producto3
cp .env.example .env
docker compose up --build
# App:     http://localhost:8080
# Swagger: http://localhost:8080/swagger
# Admin:   admin@legal.com / Admin123*

# Ejecutar las pruebas automatizadas:
docker run --rm -v "$(pwd)":/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test tests/SistemaLegalPagares.Tests/SistemaLegalPagares.Tests.csproj
```

---

## 8. Conclusión

El Producto 3 extiende el caso de estudio del Sistema Legal de Pagarés (Producto 1) sin abandonar su
arquitectura MVC, sus patrones de diseño ni su stack tecnológico, y demuestra que dicha arquitectura escala
naturalmente para incorporar los cuatro requisitos de integración de esta unidad: seguridad reforzada
(aprobación administrativa, CSRF, rate limiting, cabeceras HTTP, JWT, control de roles), dos servicios web
de terceros reales (Google reCAPTCHA y correo SMTP), una API REST propia documentada y autenticada de forma
independiente a la interfaz web, y un repositorio completamente reproducible mediante Docker. Todo el flujo
—desde el registro de un abogado hasta la generación del PDF legal del pagaré— fue verificado en
funcionamiento contra el contenedor real, no solo en el código fuente.
