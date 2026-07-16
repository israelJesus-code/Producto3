namespace SistemaLegalPagares.Models;

/// <summary>Tabla intermedia N:M entre Pagare y Deudor (llave compuesta).</summary>
public class PagareDeudor
{
    public int PagareId { get; set; }
    public Pagare? Pagare { get; set; }

    public int DeudorId { get; set; }
    public Deudor? Deudor { get; set; }
}
