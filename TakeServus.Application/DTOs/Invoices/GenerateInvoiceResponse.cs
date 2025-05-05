namespace TakeServus.Application.DTOs.Invoices;

public class GenerateInvoiceResponse
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
}