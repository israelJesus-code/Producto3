using QuestPDF.Fluent;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Pdf;

namespace SistemaLegalPagares.Tests.Services.Pdf;

public class PagarePdfDocumentTests
{
    private static Pagare CrearPagareValido() => new()
    {
        Id = 1,
        ExpedienteId = 1,
        Expediente = new Expediente { Id = 1, NumeroExpediente = "EXP-2026-001" },
        LugarExpedicion = "Puebla, Puebla",
        FechaExpedicion = DateTime.UtcNow,
        Acreedor = "Joel Gomez Herrera",
        MontoTotal = 50000m,
        MontoLetra = "CINCUENTA MIL PESOS 00/100 M.N.",
        FechaVencimiento = DateTime.UtcNow.AddMonths(1),
        LugarPagoPagare = "Puebla, Puebla",
        InteresMoratorio = 5,
        SerieDesde = 1,
        SerieHasta = 1,
    };

    [Fact]
    public void GeneratePdf_ConPagareValido_GeneraDocumentoNoVacio()
    {
        var pagare = CrearPagareValido();
        var deudores = new List<Deudor>
        {
            new() { Id = 1, NombreCompleto = "Joel Gomez Herrera", Direccion = "Av. 9 Pte. 106", Poblacion = "Puebla" },
        };

        var document = new PagarePdfDocument(pagare, deudores);
        var bytes = document.GeneratePdf();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        // Cabecera %PDF- identifica un documento PDF válido.
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void GeneratePdf_SinDeudores_NoLanzaExcepcion()
    {
        var pagare = CrearPagareValido();
        var document = new PagarePdfDocument(pagare, new List<Deudor>());

        var exception = Record.Exception(() => document.GeneratePdf());

        Assert.Null(exception);
    }
}
