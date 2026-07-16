using Microsoft.AspNetCore.Identity;

namespace SistemaLegalPagares.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Controla el flujo de aprobación administrativa: un abogado registrado
    /// no puede iniciar sesión hasta que un Admin lo aprueba.
    /// </summary>
    public bool EstaAprobado { get; set; } = false;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
