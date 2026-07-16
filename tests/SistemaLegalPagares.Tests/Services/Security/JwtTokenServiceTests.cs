using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Security;

namespace SistemaLegalPagares.Tests.Services.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CrearServicio()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "clave-de-prueba-de-al-menos-32-caracteres-1234",
                ["Jwt:Issuer"] = "SistemaLegalPagares",
                ["Jwt:Audience"] = "SistemaLegalPagaresApi",
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_IncluyeClaimsDeUsuarioYRoles()
    {
        var service = CrearServicio();
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "abogado@example.com",
            NombreCompleto = "Abogado de Prueba",
        };

        var (token, expiresAtUtc) = service.GenerateToken(user, new List<string> { "Abogado" });

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAtUtc > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Abogado");
        Assert.Equal("SistemaLegalPagares", jwt.Issuer);
    }
}
