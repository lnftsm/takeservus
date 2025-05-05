namespace TakeServus.Application.DTOs.Invoices;

public class CreateInvoiceRequest
{
    public Guid JobId { get; set; }

    public List<InvoiceMaterialDto> Materials { get; set; } = new();
}

public class InvoiceMaterialDto
{
    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}