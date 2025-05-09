using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Invoices;
using TakeServus.Domain.Entities;
using TakeServus.Infrastructure.Pdf;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public InvoiceController(TakeServusDbContext context)
    {
        _context = context;
    }

    // Dispatcher manually creates invoice
    [HttpPost]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var job = await _context.Jobs
            .Include(j => j.Invoice)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId);

        if (job == null)
            return NotFound("Job not found.");

        if (job.Invoice != null)
            return BadRequest("Invoice already exists.");

        var total = request.Materials.Sum(m => m.Quantity * m.UnitPrice);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Amount = total,
            CreatedAt = DateTime.UtcNow,
            IsPaid = false
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            invoice.Id,
            invoice.Amount,
            invoice.IsPaid,
            invoice.CreatedAt,
            Customer = job.Customer.FullName
        });
    }

    // Auto-generate from JobMaterials
    [HttpPost("{jobId}/generate")]
    [Authorize(Roles = "Owner,Dispatcher,Technician")]
    public async Task<ActionResult<GenerateInvoiceResponse>> GenerateInvoice(Guid jobId)
    {
        var job = await _context.Jobs
            .Include(j => j.JobMaterials)
                .ThenInclude(jm => jm.Material)
            .Include(j => j.Invoice)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return NotFound("Job not found.");

        if (job.Invoice != null)
            return BadRequest("Invoice already exists.");

        var totalAmount = job.JobMaterials.Sum(jm => jm.QuantityUsed * jm.Material.UnitPrice);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Amount = totalAmount,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return Ok(new GenerateInvoiceResponse
        {
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            IsPaid = invoice.IsPaid,
            CreatedAt = invoice.CreatedAt
        });
    }

    // Mark invoice as paid
    [HttpPut("{invoiceId}/pay")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> MarkAsPaid(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null)
            return NotFound("Invoice not found.");

        invoice.IsPaid = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Download PDF
    [HttpGet("{invoiceId}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Job)
                .ThenInclude(j => j.Customer)
            .Include(i => i.Job)
                .ThenInclude(j => j.JobMaterials)
                    .ThenInclude(jm => jm.Material)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return NotFound("Invoice not found.");

        var materialList = invoice.Job.JobMaterials
            .Select(jm => (jm.Material.Name, jm.QuantityUsed, jm.Material.UnitPrice))
            .ToList();

        var customerName = invoice.Job.Customer.FullName;
        var pdf = InvoicePdfGenerator.Generate(invoice, customerName, materialList);

        return File(pdf, "application/pdf", $"invoice-{invoice.Id}.pdf", enableRangeProcessing: true);
    }

    // Paginated list with filters and sorting
    [HttpGet("list")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<ActionResult<TakeServus.Application.DTOs.Common.PagedResult<InvoiceResponse>>> GetInvoices(
        [FromQuery] Guid? customerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? sortBy,
        [FromQuery] bool desc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Job)
                .ThenInclude(j => j.Customer)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(i => i.Job.CustomerId == customerId);
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(i => i.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(i => i.CreatedAt <= end);
        }

        var totalCount = await query.CountAsync();

        string sortProperty = !string.IsNullOrWhiteSpace(sortBy) ? sortBy : nameof(Invoice.CreatedAt);
        string sortOrder = desc ? "descending" : "ascending";

        var invoices = await query
            .OrderBy($"{sortProperty} {sortOrder}")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceResponse
            {
                Id = i.Id,
                JobId = i.JobId,
                CustomerName = i.Job.Customer.FullName,
                Amount = i.Amount,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(new TakeServus.Application.DTOs.Common.PagedResult<InvoiceResponse>
        {
            Items = invoices,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}