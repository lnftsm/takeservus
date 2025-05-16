using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeServus.Application.DTOs.Invoices;
using TakeServus.Application.Interfaces;
using TakeServus.Application.DTOs.Common;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var invoiceId = await _invoiceService.CreateInvoiceAsync(request);
            return Ok(new { invoiceId });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{jobId}/generate")]
    [Authorize(Roles = "Owner,Dispatcher,Technician")]
    public async Task<ActionResult<GenerateInvoiceResponse>> GenerateInvoice(Guid jobId)
    {
        try
        {
            var result = await _invoiceService.GenerateInvoiceAsync(jobId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{invoiceId}/pay")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> MarkAsPaid(Guid invoiceId)
    {
        var result = await _invoiceService.MarkAsPaidAsync(invoiceId);
        return result ? NoContent() : NotFound("Invoice not found.");
    }

    [HttpGet("{invoiceId}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId)
    {
        try
        {
            var pdf = await _invoiceService.GeneratePdfAsync(invoiceId);
            return File(pdf, "application/pdf", $"invoice-{invoiceId}.pdf", enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("list")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<ActionResult<PagedResult<InvoiceResponse>>> GetInvoices(
        [FromQuery] Guid? customerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? sortBy,
        [FromQuery] bool desc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var list = await _invoiceService.GetInvoicesAsync(
            customerId, startDate, endDate,
            sortBy, desc, page, pageSize,
            cancellationToken);

        return Ok(new PagedResult<InvoiceResponse>
        {
            Items = list,
            TotalCount = list.Count,
            Page = page,
            PageSize = pageSize
        });
    }
}