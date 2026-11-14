using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;
using IContainer = QuestPDF.Infrastructure.IContainer;
using System.Globalization;
using System.Reflection;

namespace CLI.Commands;

internal static class PeoplePdf
{
    static PeoplePdf()
    {
        // QuestPDF Community license: free for organisations under US$1M annual revenue, see https://www.questpdf.com/license/.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(IReadOnlyList<PersonSummary> people)
    {
        // Header uses each PersonSummary property's [Description] when present, otherwise the property name.
        var headers = typeof(PersonSummary).GetProperties()
            .Select(p => p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name)
            .ToArray();

        return Document.Create(document => document.Page(page =>
        {
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().PaddingBottom(10).Text("People").FontSize(18).Bold();

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                });

                // Header row repeats on every page.
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).AlignRight().Text(headers[0]);
                    header.Cell().Element(HeaderCell).Text(headers[1]);
                    header.Cell().Element(HeaderCell).Text(headers[2]);
                    header.Cell().Element(HeaderCell).AlignRight().Text(headers[3]);
                });

                foreach (var person in people)
                {
                    table.Cell().Element(BodyCell).AlignRight().Text(person.Id.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Element(BodyCell).Text(person.Name);
                    table.Cell().Element(BodyCell).Text(person.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    table.Cell().Element(BodyCell).AlignRight().Text(person.Salary.ToString("0.00", CultureInfo.InvariantCulture));
                }
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(x => x.Bold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);
}
