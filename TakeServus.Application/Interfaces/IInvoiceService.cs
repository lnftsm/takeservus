using TakeServus.Application.DTOs.Invoices;

namespace TakeServus.Application.Interfaces;

public interface IInvoiceService
{
  Task<GenerateInvoiceResponse> GenerateInvoiceAsync(Guid jobId);
  Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request);
  Task<bool> MarkAsPaidAsync(Guid invoiceId);
  Task<byte[]> GeneratePdfAsync(Guid invoiceId);
  Task<List<InvoiceResponse>> GetInvoicesAsync(
      Guid? customerId, DateOnly? startDate, DateOnly? endDate,
      string? sortBy, bool desc, int page, int pageSize,
      CancellationToken cancellationToken);
}