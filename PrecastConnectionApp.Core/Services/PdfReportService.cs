using System;
using System.IO;
using PrecastConnectionApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PrecastConnectionApp.Services
{
    public class PdfReportService
    {
        public void GenerateCalculationReport(CalculationResult result, string outputPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, result));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(outputPath);
        }

        private void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);

            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Precast Horizontal Connection Report").Style(titleStyle);
                    column.Item().Text($"Generated on: {DateTime.Now:d}");
                });
            });
        }

        private void ComposeContent(IContainer container, CalculationResult result)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);

                column.Item().Text($"Story: {result.Story}").FontSize(14);
                column.Item().Text($"Column/Pier Label: {result.ColumnLabel}").FontSize(14);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Parameter");
                        header.Cell().Element(CellStyle).Text("Value");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    table.Cell().Element(CellStyle).Text("Calculated Pt");
                    table.Cell().Element(CellStyle).Text($"{result.CalculatedPt:F2} kN");

                    table.Cell().Element(CellStyle).Text("Calculated Metey");
                    table.Cell().Element(CellStyle).Text($"{result.CalculatedMetey:F2} kNm");

                    table.Cell().Element(CellStyle).Text("Status");
                    table.Cell().Element(CellStyle).Text(result.IsPass ? "PASS" : "FAIL").FontColor(result.IsPass ? Colors.Green.Medium : Colors.Red.Medium);

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });

                column.Item().Text("Note: This is a programmatically generated summary representing the original Excel 'Calculation sheet'.").Italic();
            });
        }
    }
}
