using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaLegalPagares.Models;

/// <summary>Representa cada pagaré individual dentro de una serie numerada.</summary>
public class SubPagare
{
    public int Id { get; set; }

    public int PagareId { get; set; }
    public Pagare? Pagare { get; set; }

    public int Numero { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    public DateTime FechaVencimiento { get; set; }
}
