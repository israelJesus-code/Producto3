using Microsoft.AspNetCore.Identity;
using Moq;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Tests.TestHelpers;

/// <summary>Crea un UserManager&lt;ApplicationUser&gt; mockeable (Moq) respaldado por un store en memoria.</summary>
public static class MockUserManagerFactory
{
    public static Mock<UserManager<ApplicationUser>> Create()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }
}
