using System.ComponentModel.DataAnnotations;

namespace SistemaLegalPagares.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required, Display(Name = "Nombre Completo")]
    [StringLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Display(Name = "CURP"), StringLength(18)]
    public string? CURP { get; set; }

    [Display(Name = "INE"), StringLength(30)]
    public string? INE { get; set; }

    [Display(Name = "RFC"), StringLength(13)]
    public string? RFC { get; set; }

    [Display(Name = "Teléfono"), StringLength(20)]
    public string? Telefono { get; set; }

    [Display(Name = "Correo"), EmailAddress, StringLength(150)]
    public string? Correo { get; set; }

    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Fecha de Registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Expediente> Expedientes { get; set; } = new List<Expediente>();
}
