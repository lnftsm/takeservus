using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using TakeServus.Application.DTOs.Invoices;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Infrastructure.Pdf;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
  private readonly TakeServusDbContext _context;

  public InvoiceService(TakeServusDbContext context)
  {
    _context = context;
  }

  public async Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request)
  {
    var job = await _context.Jobs
        .Include(j => j.Invoice)
        .Include(j => j.Customer)
        .FirstOrDefaultAsync(j => j.Id == request.JobId);

    if (job == null || job.Invoice != null)
      throw new InvalidOperationException("Job not found or invoice already exists.");

    var total = request.Materials.Sum(m => m.Quantity * m.UnitPrice);

    var invoice = new Invoice
    {
      Id = Guid.NewGuid(),
      JobId = request.JobId,
      Amount = total,
      CreatedAt = DateTime.UtcNow,
      IsPaid = false
    };

    _context.Invoices.Add(invoice);
    await _context.SaveChangesAsync();

    return invoice.Id;
  }

  public async Task<GenerateInvoiceResponse> GenerateInvoiceAsync(Guid jobId)
  {
    var job = await _context.Jobs
        .Include(j => j.JobMaterials).ThenInclude(jm => jm.Material)
        .Include(j => j.Invoice)
        .FirstOrDefaultAsync(j => j.Id == jobId);

    if (job == null || job.Invoice != null)
      throw new InvalidOperationException("Job not found or invoice already exists.");

    var total = job.JobMaterials.Sum(jm => jm.QuantityUsed * jm.Material.UnitPrice);

    var invoice = new Invoice
    {
      Id = Guid.NewGuid(),
      JobId = job.Id,
      Amount = total,
      IsPaid = false,
      CreatedAt = DateTime.UtcNow
    };

    _context.Invoices.Add(invoice);
    await _context.SaveChangesAsync();

    return new GenerateInvoiceResponse
    {
      InvoiceId = invoice.Id,
      Amount = invoice.Amount,
      IsPaid = invoice.IsPaid,
      CreatedAt = invoice.CreatedAt
    };
  }

  public async Task<bool> MarkAsPaidAsync(Guid invoiceId)
  {
    var invoice = await _context.Invoices.FindAsync(invoiceId);
    if (invoice == null) return false;

    invoice.IsPaid = true;
    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<byte[]> GeneratePdfAsync(Guid invoiceId)
  {
    var invoice = await _context.Invoices
        .Include(i => i.Job).ThenInclude(j => j.Customer)
        .Include(i => i.Job).ThenInclude(j => j.JobMaterials).ThenInclude(jm => jm.Material)
        .FirstOrDefaultAsync(i => i.Id == invoiceId);

    if (invoice == null) throw new InvalidOperationException("Invoice not found");

    var materials = invoice.Job.JobMaterials
        .Select(jm => (jm.Material.Name, jm.QuantityUsed, jm.Material.UnitPrice))
        .ToList();

    var customer = invoice.Job.Customer.FullName;
    return InvoicePdfGenerator.Generate(invoice, customer, materials);
  }

  public async Task<List<InvoiceResponse>> GetInvoicesAsync(
      Guid? customerId, DateOnly? startDate, DateOnly? endDate,
      string? sortBy, bool desc, int page, int pageSize,
      CancellationToken cancellationToken)
  {
    var query = _context.Invoices
        .Include(i => i.Job).ThenInclude(j => j.Customer)
        .AsQueryable();

    if (customerId.HasValue)
      query = query.Where(i => i.Job.CustomerId == customerId.Value);

    if (startDate.HasValue)
      query = query.Where(i => i.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));

    if (endDate.HasValue)
      query = query.Where(i => i.CreatedAt <= endDate.Value.ToDateTime(TimeOnly.MaxValue));

    string sortField = !string.IsNullOrWhiteSpace(sortBy) ? sortBy : nameof(Invoice.CreatedAt);
    string direction = desc ? "descending" : "ascending";

    var result = await query
        .OrderBy($"{sortField} {direction}")
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(i => new InvoiceResponse
        {
          Id = i.Id,
          JobId = i.JobId,
          CustomerName = i.Job.Customer.FullName,
          Amount = i.Amount,
          CreatedAt = i.CreatedAt
        }).ToListAsync(cancellationToken);

    return result;
  }
}