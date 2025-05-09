using System;

namespace TakeServus.Application.DTOs.Invoices;

public class InvoiceResponse
{
  public Guid Id { get; set; }
  public Guid JobId { get; set; }
  public string CustomerName { get; set; } = default!;
  public decimal Amount { get; set; }
  public DateTime CreatedAt { get; set; }
}