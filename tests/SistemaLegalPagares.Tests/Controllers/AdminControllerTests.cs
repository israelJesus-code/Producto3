using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaLegalPagares.Controllers;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Email;
using SistemaLegalPagares.Tests.TestHelpers;

namespace SistemaLegalPagares.Tests.Controllers;

public class AdminControllerTests
{
    private static (AdminController controller, Mock<UserManager<ApplicationUser>> userManagerMock) CrearController()
    {
        var userManagerMock = MockUserManagerFactory.Create();
        var emailSenderMock = new Mock<IAppEmailSender>();
        var loggerMock = new Mock<ILogger<AdminController>>();

        var httpContext = new DefaultHttpContext();
        var controller = new AdminController(userManagerMock.Object, emailSenderMock.Object, loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
        return (controller, userManagerMock);
    }

    [Fact]
    public async Task Aprobar_EstableceEstaAprobadoYAsignaRolAbogado()
    {
        var (controller, userManagerMock) = CrearController();
        var usuario = new ApplicationUser { Id = "u1", Email = "abogado@example.com", EstaAprobado = false };

        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(usuario);
        userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(m => m.IsInRoleAsync(usuario, "Abogado")).ReturnsAsync(false);
        userManagerMock.Setup(m => m.AddToRoleAsync(usuario, "Abogado")).ReturnsAsync(IdentityResult.Success);

        await controller.Aprobar("u1");

        Assert.True(usuario.EstaAprobado);
        userManagerMock.Verify(m => m.AddToRoleAsync(usuario, "Abogado"), Times.Once);
    }

    [Fact]
    public async Task Rechazar_EliminaAlUsuarioDelSistema()
    {
        var (controller, userManagerMock) = CrearController();
        var usuario = new ApplicationUser { Id = "u2", Email = "pendiente@example.com" };

        userManagerMock.Setup(m => m.FindByIdAsync("u2")).ReturnsAsync(usuario);
        userManagerMock.Setup(m => m.DeleteAsync(usuario)).ReturnsAsync(IdentityResult.Success);

        await controller.Rechazar("u2");

        userManagerMock.Verify(m => m.DeleteAsync(usuario), Times.Once);
    }
}
