using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Services.Pdf;

/// <summary>
/// Genera el PDF del pagaré replicando el formato visual "Formitec" con todos
/// los campos exigidos por el Art. 170 de la LGTOC.
/// </summary>
public class PagarePdfDocument : IDocument
{
    private readonly Pagare _pagare;
    private readonly IReadOnlyList<Deudor> _deudores;

    public PagarePdfDocument(Pagare pagare, IReadOnlyList<Deudor> deudores)
    {
        _pagare = pagare;
        _deudores = deudores;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Content().Border(1).Background(Colors.Green.Lighten5).Padding(15).Column(col =>
            {
                col.Spacing(8);

                col.Item().Row(row =>
                {
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text("PAGARÉ").Bold().FontSize(16);
                        c.Item().Text($"No. {_pagare.SerieDesde} de {_pagare.SerieHasta}");
                    });
                    row.RelativeItem(3).Column(c =>
                    {
                        c.Item().Text("LUGAR DE EXPEDICIÓN").FontSize(8).SemiBold();
                        c.Item().Text(_pagare.LugarExpedicion);
                    });
                    row.RelativeItem(3).Table(table =>
                    {
                        table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        table.Cell().Text("DÍA").FontSize(8).SemiBold();
                        table.Cell().Text("MES").FontSize(8).SemiBold();
                        table.Cell().Text("AÑO").FontSize(8).SemiBold();
                        table.Cell().Text(_pagare.FechaExpedicion.Day.ToString());
                        table.Cell().Text(_pagare.FechaExpedicion.Month.ToString());
                        table.Cell().Text(_pagare.FechaExpedicion.Year.ToString());
                    });
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text("BUENO POR").FontSize(8).SemiBold();
                        c.Item().Text($"$ {_pagare.MontoTotal:N2}").Bold();
                    });
                });

                col.Item().Text(text =>
                {
                    text.Span("Debo(emos) y pagaré(mos) incondicionalmente sin protesto este pagaré en el lugar y fechas citadas donde elija el tenedor el día de su vencimiento a la orden de ");
                    text.Span(_pagare.Acreedor).Bold();
                    text.Span(" en ");
                    text.Span(_pagare.LugarPagoPagare ?? _pagare.LugarExpedicion).Bold();
                    text.Span(" el día ");
                    text.Span(_pagare.FechaVencimiento.ToString("dd/MM/yyyy")).Bold();
                    text.Span(".");
                });

                col.Item().Text(text =>
                {
                    text.Span("La cantidad de: ").SemiBold();
                    text.Span(_pagare.MontoLetra.ToUpperInvariant()).Bold();
                });

                col.Item().DefaultTextStyle(x => x.FontSize(8)).Text(text =>
                {
                    text.Span("VALOR RECIBIDO A MI (NUESTRA) ENTERA SATISFACCIÓN. ESTE PAGARÉ FORMA PARTE DE UNA SERIE NUMERADA DEL ");
                    text.Span(_pagare.SerieDesde.ToString()).Bold();
                    text.Span(" AL ");
                    text.Span(_pagare.SerieHasta.ToString()).Bold();
                    text.Span(" Y TODOS ESTÁN SUJETOS A LA CONDICIÓN DE QUE NO PAGARSE CUALQUIERA DE ELLOS A SU VENCIMIENTO, SERÁN EXIGIBLES TODOS LOS QUE LE SIGUEN EN NÚMERO, ADEMÁS DE LOS YA VENCIDOS. CAUSARÁN INTERESES MORATORIOS DEL ");
                    text.Span($"{_pagare.InteresMoratorio:0.00}%").Bold();
                    text.Span(" POR CADA MES O FRACCIÓN, CONFORME A LO DISPUESTO POR EL ART. 174 Y 152 DE LA LEY GENERAL DE TÍTULOS Y OPERACIONES DE CRÉDITO.");
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(2);
                    });

                    table.Cell().Border(1).Padding(4).Text("NOMBRE Y DATOS DEL DEUDOR").SemiBold().FontSize(8);
                    table.Cell().Border(1).Padding(4).AlignCenter().Text("AVAL").SemiBold().FontSize(8);
                    table.Cell().Border(1).Padding(4).AlignCenter().Text("DEUDOR").SemiBold().FontSize(8);

                    var deudorPrincipal = _deudores.FirstOrDefault();
                    table.Cell().Border(1).Padding(4).Column(c =>
                    {
                        c.Item().Text($"NOMBRE: {deudorPrincipal?.NombreCompleto}");
                        c.Item().Text($"DOMICILIO: {deudorPrincipal?.Direccion}");
                        c.Item().Text($"POBLACIÓN: {deudorPrincipal?.Poblacion}");
                        if (_deudores.Count > 1)
                        {
                            c.Item().PaddingTop(4).Text("Deudores adicionales:").FontSize(7).Italic();
                            foreach (var extra in _deudores.Skip(1))
                            {
                                c.Item().Text($"- {extra.NombreCompleto}").FontSize(7);
                            }
                        }
                    });

                    table.Cell().Border(1).Height(70).AlignCenter().AlignMiddle().Element(e =>
                        ComposeSignature(e, _pagare.FirmaAvalBase64, "FIRMA AVAL"));

                    table.Cell().Border(1).Height(70).AlignCenter().AlignMiddle().Element(e =>
                        ComposeSignature(e, _pagare.FirmaBase64, "FIRMA DEUDOR"));
                });

                col.Item().AlignRight().Text($"Expediente: {_pagare.Expediente?.NumeroExpediente}").FontSize(8);
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Documento generado el ").FontSize(8);
                text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).SemiBold();
                text.Span(" - Sistema Legal de Pagarés").FontSize(8);
            });
        });
    }

    private static void ComposeSignature(IContainer container, string? firmaBase64, string label)
    {
        container.Column(c =>
        {
            if (!string.IsNullOrWhiteSpace(firmaBase64))
            {
                try
                {
                    var base64Data = firmaBase64.Contains(',') ? firmaBase64[(firmaBase64.IndexOf(',') + 1)..] : firmaBase64;
                    var bytes = Convert.FromBase64String(base64Data);
                    c.Item().MaxHeight(40).Image(bytes).FitArea();
                }
                catch (FormatException)
                {
                    c.Item().Text("(firma inválida)").FontSize(7);
                }
            }
            c.Item().AlignCenter().Text(label).FontSize(7);
        });
    }
}
