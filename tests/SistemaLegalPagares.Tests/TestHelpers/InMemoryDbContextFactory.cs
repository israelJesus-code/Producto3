using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;

namespace SistemaLegalPagares.Tests.TestHelpers;

public static class InMemoryDbContextFactory
{
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
