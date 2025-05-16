using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Invoices;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.UnitTests.Controllers;

public class InvoiceControllerTests
{
  [Fact]
  public async Task CreateInvoice_Should_GenerateInvoiceCorrectly()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Invoice Customer" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Invoice Tech" } };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      CustomerId = customer.Id,
      TechnicianId = technician.Id,
      Title = "Invoice Job",
      Status = "Completed",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var controller = new InvoiceController(context);

    var request = new CreateInvoiceRequest
    {
      JobId = job.Id,
      Materials = new List<CreateInvoiceRequest.MaterialItem>
            {
                new() { Name = "PVC Pipe", Quantity = 2, UnitPrice = 5.99m },
                new() { Name = "Sealant", Quantity = 1, UnitPrice = 3.50m }
            }
    };

    var result = await controller.CreateInvoice(request) as OkObjectResult;
    result.ShouldNotBeNull();
    var invoiceId = (Guid)result.Value!;
    invoiceId.ShouldNotBe(Guid.Empty);

    var invoice = await context.Invoices.FindAsync(invoiceId);
    invoice.ShouldNotBeNull();
    invoice.JobId.ShouldBe(job.Id);
    invoice.Amount.ShouldBe(2 * 5.99m + 3.50m);
  }

  [Fact]
  public async Task GetInvoices_Should_ReturnPagedList()
  {
    var context = TestDbContextFactory.Create();

    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "T1" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "C1" };

    context.Technicians.Add(technician);
    context.Customers.Add(customer);

    for (int i = 0; i < 5; i++)
    {
      var job = new Job
      {
        Id = Guid.NewGuid(),
        CustomerId = customer.Id,
        TechnicianId = technician.Id,
        Title = $"Job {i}",
        ScheduledAt = DateTime.UtcNow
      };
      context.Jobs.Add(job);
      context.Invoices.Add(new Invoice
      {
        Id = Guid.NewGuid(),
        JobId = job.Id,
        Amount = 100 + i
      });
    }

    await context.SaveChangesAsync();
    var controller = new InvoiceController(context);

    var result = await controller.GetInvoices(null, null, null, null, 1, 10) as OkObjectResult;
    result.ShouldNotBeNull();

    var paged = result.Value as Application.DTOs.Common.PagedResult<InvoiceResponse>;
    paged.ShouldNotBeNull();
    paged.Items.Count.ShouldBe(5);
    paged.TotalCount.ShouldBe(5);
  }

  [Fact]
  public async Task ExportInvoicePdf_Should_ReturnFile()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "PDF Customer" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "PDF Tech" } };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      CustomerId = customer.Id,
      TechnicianId = technician.Id,
      Title = "PDF Job",
      ScheduledAt = DateTime.UtcNow
    };
    var invoice = new Invoice
    {
      Id = Guid.NewGuid(),
      JobId = job.Id,
      Amount = 200
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    context.Invoices.Add(invoice);
    await context.SaveChangesAsync();

    var controller = new InvoiceController(context);
    var result = await controller.ExportInvoicePdf(invoice.Id) as FileContentResult;

    result.ShouldNotBeNull();
    result.ContentType.ShouldBe("application/pdf");
    result.FileDownloadName.ShouldContain("Invoice-");
    result.FileContents.ShouldNotBeEmpty();
  }
}