using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TakeServus.Domain.Entities;

namespace TakeServus.Infrastructure.Pdf;

public static class InvoicePdfGenerator
{
    public static byte[] Generate(Invoice invoice, string customerName, IEnumerable<(string Name, int Quantity, decimal UnitPrice)> materials)
    {
        var total = materials.Sum(m => m.Quantity * m.UnitPrice);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"Invoice #{invoice.Id}").FontSize(18).Bold();
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"Customer: {customerName}");
                    col.Item().Text($"Date: {invoice.CreatedAt:yyyy-MM-dd}");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(200);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Material").Bold();
                            header.Cell().Text("Qty").Bold();
                            header.Cell().Text("Unit Price").Bold();
                            header.Cell().Text("Subtotal").Bold();
                        });

                        foreach (var m in materials)
                        {
                            table.Cell().Text(m.Name);
                            table.Cell().Text(m.Quantity.ToString());
                            table.Cell().Text($"{m.UnitPrice:C}");
                            table.Cell().Text($"{m.Quantity * m.UnitPrice:C}");
                        }
                    });

                    col.Item().PaddingTop(20).AlignRight().Text($"Total: {total:C}").FontSize(14).Bold();
                });
            });
        }).GeneratePdf();
    }
}