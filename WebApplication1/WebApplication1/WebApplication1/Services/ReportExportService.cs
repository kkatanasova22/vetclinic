using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication1.Models;

namespace WebApplication1.Services;

public static class ReportExportService
{
    // Прави Excel файл с обобщение, месечни данни и търсени услуги.
    public static byte[] CreateExcel(ReportsViewModel report)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = $"VetClinic report {report.Year}";
        workbook.Properties.Author = "VetClinic";

        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell("A1").Value = $"VetClinic annual report - {report.Year}";
        summary.Range("A1:B1").Merge();
        summary.Cell("A1").Style.Font.Bold = true;
        summary.Cell("A1").Style.Font.FontSize = 18;
        summary.Cell("A3").Value = "Appointments";
        summary.Cell("B3").Value = report.AppointmentCount;
        summary.Cell("A4").Value = "Completed appointments";
        summary.Cell("B4").Value = report.CompletedAppointmentCount;
        summary.Cell("A5").Value = "Recorded treatments";
        summary.Cell("B5").Value = report.TreatmentCount;
        summary.Cell("A6").Value = "Estimated revenue (BGN)";
        summary.Cell("B6").Value = (double)report.TotalTreatmentValue;
        summary.Cell("B6").Style.NumberFormat.Format = "#,##0.00";
        StyleHeader(summary.Range("A3:A6"));
        summary.Columns().AdjustToContents();

        var monthly = workbook.Worksheets.Add("Monthly report");
        var monthlyHeaders = new[] { "Month", "Appointments", "Completed", "Treatments", "Estimated revenue (BGN)" };
        for (var column = 0; column < monthlyHeaders.Length; column++)
        {
            monthly.Cell(1, column + 1).Value = monthlyHeaders[column];
        }

        for (var index = 0; index < report.Months.Count; index++)
        {
            var row = index + 2;
            var month = report.Months[index];
            monthly.Cell(row, 1).Value = month.MonthName;
            monthly.Cell(row, 2).Value = month.AppointmentCount;
            monthly.Cell(row, 3).Value = month.CompletedAppointmentCount;
            monthly.Cell(row, 4).Value = month.TreatmentCount;
            monthly.Cell(row, 5).Value = (double)month.TreatmentValue;
        }

        StyleHeader(monthly.Range(1, 1, 1, monthlyHeaders.Length));
        monthly.Range(1, 1, report.Months.Count + 1, monthlyHeaders.Length).SetAutoFilter();
        monthly.SheetView.FreezeRows(1);
        monthly.Column(5).Style.NumberFormat.Format = "#,##0.00";
        monthly.Columns().AdjustToContents();

        var services = workbook.Worksheets.Add("Top services");
        var serviceHeaders = new[] { "Service", "Requests", "Estimated revenue (BGN)" };
        for (var column = 0; column < serviceHeaders.Length; column++)
        {
            services.Cell(1, column + 1).Value = serviceHeaders[column];
        }

        for (var index = 0; index < report.TopServices.Count; index++)
        {
            var row = index + 2;
            var service = report.TopServices[index];
            services.Cell(row, 1).Value = service.Name;
            services.Cell(row, 2).Value = service.RequestCount;
            services.Cell(row, 3).Value = (double)service.TotalValue;
        }

        StyleHeader(services.Range(1, 1, 1, serviceHeaders.Length));
        if (report.TopServices.Count > 0)
        {
            services.Range(1, 1, report.TopServices.Count + 1, serviceHeaders.Length).SetAutoFilter();
        }

        services.SheetView.FreezeRows(1);
        services.Column(3).Style.NumberFormat.Format = "#,##0.00";
        services.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Създава PDF справка с показателите и двете таблици.
    public static byte[] CreatePdf(ReportsViewModel report)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var culture = CultureInfo.GetCultureInfo("en-US");

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(9));
                page.Header().Column(header =>
                {
                    header.Item().Text($"VetClinic annual report - {report.Year}")
                        .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                    header.Item().Text("Estimated revenue is calculated from recorded procedure prices.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(16).Column(content =>
                {
                    content.Spacing(12);
                    content.Item().Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Appointments", report.AppointmentCount.ToString(culture));
                        SummaryCard(row.RelativeItem(), "Completed appointments", report.CompletedAppointmentCount.ToString(culture));
                        SummaryCard(row.RelativeItem(), "Recorded treatments", report.TreatmentCount.ToString(culture));
                        SummaryCard(row.RelativeItem(), "Estimated revenue", $"{report.TotalTreatmentValue.ToString("N2", culture)} BGN");
                    });

                    content.Item().Text("Monthly revenue").FontSize(14).Bold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Month");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Appointments");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Completed");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Treatments");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Estimated revenue");
                        });

                        foreach (var month in report.Months)
                        {
                            table.Cell().Element(BodyCell).Text(month.MonthName);
                            table.Cell().Element(BodyCell).AlignRight().Text(month.AppointmentCount.ToString(culture));
                            table.Cell().Element(BodyCell).AlignRight().Text(month.CompletedAppointmentCount.ToString(culture));
                            table.Cell().Element(BodyCell).AlignRight().Text(month.TreatmentCount.ToString(culture));
                            table.Cell().Element(BodyCell).AlignRight().Text($"{month.TreatmentValue.ToString("N2", culture)} BGN");
                        }
                    });

                    content.Item().PaddingTop(4).Text("Most requested services").FontSize(14).Bold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Service");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Requests");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Estimated revenue");
                        });

                        if (report.TopServices.Count == 0)
                        {
                            table.Cell().ColumnSpan(3).Element(BodyCell).Text("No treatments were recorded for this year.");
                        }
                        else
                        {
                            foreach (var service in report.TopServices)
                            {
                                table.Cell().Element(BodyCell).Text(service.Name);
                                table.Cell().Element(BodyCell).AlignRight().Text(service.RequestCount.ToString(culture));
                                table.Cell().Element(BodyCell).AlignRight().Text($"{service.TotalValue.ToString("N2", culture)} BGN");
                            }
                        }
                    });
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span($"Generated {DateTime.Today.ToString("MMMM d, yyyy", culture)} · Page ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#344d67");
    }

    private static IContainer HeaderCell(IContainer container) => container
        .Background(Colors.Blue.Darken2)
        .DefaultTextStyle(style => style.FontColor(Colors.White).SemiBold())
        .Padding(6);

    private static IContainer BodyCell(IContainer container) => container
        .BorderBottom(0.5f)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(6);

    private static void SummaryCard(IContainer container, string label, string value)
    {
        container
            .Background(Colors.Grey.Lighten4)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                column.Item().Text(value).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
            });
    }
}
