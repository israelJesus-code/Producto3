using System.ComponentModel.DataAnnotations;

namespace SistemaLegalPagares.Models;

public class Expediente
{
    public int Id { get; set; }

    [Required, Display(Name = "Número de Expediente")]
    [StringLength(50)]
    public string NumeroExpediente { get; set; } = string.Empty;

    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [Display(Name = "Fecha de Creación")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<Pagare> Pagares { get; set; } = new List<Pagare>();
}
