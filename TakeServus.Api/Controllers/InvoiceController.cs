using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // Mark as paid
    [HttpPut("{invoiceId}/pay")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> MarkAsPaid(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null) return NotFound();

        invoice.IsPaid = true;
        await _context.SaveChangesAsync();

        return Ok();
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
            return NotFound();

        var materialList = invoice.Job.JobMaterials
            .Select(jm => (jm.Material.Name, jm.QuantityUsed, jm.Material.UnitPrice))
            .ToList();

        var customerName = invoice.Job.Customer.FullName;

        var pdf = InvoicePdfGenerator.Generate(invoice, customerName, materialList);

        return File(pdf, "application/pdf", $"invoice-{invoice.Id}.pdf");
    }
}