using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SistemaLegalPagares.Controllers;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Tests.TestHelpers;

namespace SistemaLegalPagares.Tests.Controllers;

public class PagaresControllerTests
{
    private static PagaresController CrearController(
        out Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> userManagerMock,
        string usuarioId = "abogado-1",
        SistemaLegalPagares.Data.ApplicationDbContext? sharedContext = null)
    {
        var context = sharedContext ?? InMemoryDbContextFactory.Create();
        if (sharedContext is null)
        {
            context.Expedientes.Add(new Expediente { Id = 1, NumeroExpediente = "EXP-2026-001" });
            context.SaveChanges();
        }

        userManagerMock = MockUserManagerFactory.Create();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(usuarioId);

        var controller = new PagaresController(context, userManagerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Abogado") }, "test")),
                },
            },
        };
        return controller;
    }

    private static Pagare PagareValido() => new()
    {
        ExpedienteId = 1,
        LugarExpedicion = "Puebla, Puebla",
        FechaExpedicion = DateTime.UtcNow,
        Acreedor = "Joel Gomez Herrera",
        MontoTotal = 50000m,
        MontoLetra = "CINCUENTA MIL PESOS 00/100 M.N.",
        FechaVencimiento = DateTime.UtcNow.AddMonths(1),
        SerieDesde = 1,
        SerieHasta = 1,
    };

    [Fact]
    public async Task Create_ConModeloValidoYExpedienteExistente_GuardaYRedirigeADetails()
    {
        var controller = CrearController(out _);

        var resultado = await controller.Create(PagareValido(), deudorIds: null);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(PagaresController.Details), redirect.ActionName);
    }

    [Fact]
    public async Task Create_ConFechaVencimientoAnteriorAHoy_AgregaModelErrorYRetornaVista()
    {
        var controller = CrearController(out _);
        var pagare = PagareValido();
        pagare.FechaVencimiento = DateTime.UtcNow.AddDays(-5);

        var resultado = await controller.Create(pagare, deudorIds: null);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState[nameof(Pagare.FechaVencimiento)]!.Errors.Count > 0);
    }

    [Fact]
    public async Task Create_ConExpedienteIdCero_AgregaModelErrorNoSeRecibioElExpediente()
    {
        var controller = CrearController(out _);
        var pagare = PagareValido();
        pagare.ExpedienteId = 0;

        var resultado = await controller.Create(pagare, deudorIds: null);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("No se recibió el expediente"));
    }

    [Fact]
    public async Task Pdf_ConPagareExistente_RetornaArchivoConContentTypeApplicationPdf()
    {
        var controller = CrearController(out _, usuarioId: "abogado-1");
        var creado = await controller.Create(PagareValido(), deudorIds: null);
        var pagareId = ((RedirectToActionResult)creado).RouteValues!["id"];

        var resultado = await controller.Pdf((int)pagareId!);

        var fileResult = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.True(fileResult.FileContents.Length > 0);
    }

    [Fact]
    public async Task Details_DeOtroAbogado_RetornaForbid()
    {
        var context = InMemoryDbContextFactory.Create();
        context.Expedientes.Add(new Expediente { Id = 1, NumeroExpediente = "EXP-2026-001" });
        context.SaveChanges();

        var controller = CrearController(out _, usuarioId: "abogado-1", sharedContext: context);
        var creado = await controller.Create(PagareValido(), deudorIds: null);
        var pagareId = (int)((RedirectToActionResult)creado).RouteValues!["id"]!;

        // Un segundo abogado (distinto UsuarioId, sin rol Admin) intenta ver el pagaré ajeno,
        // usando la misma base de datos en memoria donde ya existe el pagaré del primero.
        var otroController = CrearController(out _, usuarioId: "abogado-2", sharedContext: context);
        var resultado = await otroController.Details(pagareId);

        Assert.IsType<ForbidResult>(resultado);
    }
}
