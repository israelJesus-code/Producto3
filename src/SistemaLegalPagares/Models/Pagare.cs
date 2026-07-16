using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaLegalPagares.Models;

/// <summary>
/// Captura todos los campos exigidos por el Art. 170 de la LGTOC para un pagaré,
/// con el formato visual "Formitec" usado en el despacho.
/// </summary>
public class Pagare
{
    public int Id { get; set; }

    [Required]
    public int ExpedienteId { get; set; }
    public Expediente? Expediente { get; set; }

    [Required, Display(Name = "Lugar de Expedición"), StringLength(150)]
    public string LugarExpedicion { get; set; } = string.Empty;

    [Display(Name = "Fecha de Expedición")]
    public DateTime FechaExpedicion { get; set; } = DateTime.UtcNow;

    [Required, Display(Name = "Acreedor / Beneficiario"), StringLength(200)]
    public string Acreedor { get; set; } = string.Empty;

    [Required, Column(TypeName = "decimal(18,2)"), Display(Name = "Monto Total")]
    public decimal MontoTotal { get; set; }

    [Required, Display(Name = "Monto en Letra"), StringLength(300)]
    public string MontoLetra { get; set; } = string.Empty;

    [Required, Display(Name = "Fecha de Vencimiento")]
    public DateTime FechaVencimiento { get; set; }

    [Display(Name = "Beneficiario"), StringLength(200)]
    public string? Beneficiario { get; set; }

    [Display(Name = "Lugar de Pago"), StringLength(150)]
    public string? LugarPagoPagare { get; set; }

    [Column(TypeName = "decimal(5,2)"), Display(Name = "Interés Moratorio (%)")]
    public decimal InteresMoratorio { get; set; }

    [Display(Name = "Serie Desde")]
    public int SerieDesde { get; set; } = 1;

    [Display(Name = "Serie Hasta")]
    public int SerieHasta { get; set; } = 1;

    [Display(Name = "Firma del Deudor")]
    public string? FirmaBase64 { get; set; }

    [Display(Name = "Firma del Aval")]
    public string? FirmaAvalBase64 { get; set; }

    // Se asigna en el controlador tras el model binding (usuario autenticado que crea el pagaré);
    // sin [Required] porque en el POST de creación todavía no tiene valor cuando ModelState se evalúa.
    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<SubPagare> SubPagares { get; set; } = new List<SubPagare>();
    public ICollection<PagareDeudor> PagareDeudores { get; set; } = new List<PagareDeudor>();
}
