using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.Tests.Controllers;

public class DashboardControllerTests
{
  [Fact]
  public async Task GetSummary_Should_ReturnCorrectCounts()
  {
    var context = TestDbContextFactory.Create();

    var user = new User { Id = Guid.NewGuid(), FullName = "Dispatcher", Role = "Dispatcher", IsActive = true };
    var tech = new Technician { Id = Guid.NewGuid(), User = user };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Dashboard Customer" };
    var material = new Material { Id = Guid.NewGuid(), Name = "Cable", StockQuantity = 5 };

    context.Users.Add(user);
    context.Technicians.Add(tech);
    context.Customers.Add(customer);
    context.Materials.Add(material);

    context.Jobs.AddRange(
        new Job { Id = Guid.NewGuid(), Status = "Scheduled", TechnicianId = tech.Id, CustomerId = customer.Id, ScheduledAt = DateTime.UtcNow },
        new Job { Id = Guid.NewGuid(), Status = "Completed", TechnicianId = tech.Id, CustomerId = customer.Id, ScheduledAt = DateTime.UtcNow },
        new Job { Id = Guid.NewGuid(), Status = "EnRoute", TechnicianId = tech.Id, CustomerId = customer.Id, ScheduledAt = DateTime.UtcNow }
    );

    await context.SaveChangesAsync();

    var controller = new DashboardController(context);
    var result = await controller.GetSummary() as OkObjectResult;
    result.ShouldNotBeNull();

    dynamic summary = result.Value!;
    summary.TotalJobs.ShouldBe(3);
    summary.ScheduledJobs.ShouldBe(1);
    summary.EnRouteJobs.ShouldBe(1);
    summary.CompletedJobs.ShouldBe(1);
    summary.ActiveTechnicians.ShouldBe(1);
    summary.TotalCustomers.ShouldBe(1);
    ((int)summary.LowStockMaterials.Count).ShouldBe(1);
  }

  [Fact]
  public async Task GetJobSummary_Should_GroupByStatus()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Summary Customer" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Summary Tech" } };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.AddRange(
        new Job { Id = Guid.NewGuid(), Status = "Scheduled", TechnicianId = technician.Id, CustomerId = customer.Id },
        new Job { Id = Guid.NewGuid(), Status = "Scheduled", TechnicianId = technician.Id, CustomerId = customer.Id },
        new Job { Id = Guid.NewGuid(), Status = "Completed", TechnicianId = technician.Id, CustomerId = customer.Id }
    );

    await context.SaveChangesAsync();

    var controller = new DashboardController(context);
    var result = await controller.GetJobSummary() as OkObjectResult;
    result.ShouldNotBeNull();

    dynamic data = result.Value!;
    data.ShouldContain(x => x.Status == "Scheduled" && x.Count == 2);
    data.ShouldContain(x => x.Status == "Completed" && x.Count == 1);
  }

  [Fact]
  public async Task GetRevenueSummary_Should_ReturnGroupedTotals()
  {
    var context = TestDbContextFactory.Create();

    context.Invoices.AddRange(
        new Invoice { Id = Guid.NewGuid(), Amount = 100, CreatedAt = new DateTime(2025, 1, 10) },
        new Invoice { Id = Guid.NewGuid(), Amount = 150, CreatedAt = new DateTime(2025, 1, 20) },
        new Invoice { Id = Guid.NewGuid(), Amount = 200, CreatedAt = new DateTime(2025, 2, 5) }
    );

    await context.SaveChangesAsync();

    var controller = new DashboardController(context);
    var result = await controller.GetRevenueSummary() as OkObjectResult;
    result.ShouldNotBeNull();

    dynamic list = result.Value!;
    list.ShouldContain(x => x.Month == 1 && x.Year == 2025 && x.TotalRevenue == 250);
    list.ShouldContain(x => x.Month == 2 && x.Year == 2025 && x.TotalRevenue == 200);
  }

  [Fact]
  public async Task GetTechnicianActivity_Should_ListCompletedCounts()
  {
    var context = TestDbContextFactory.Create();

    var tech1 = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech One" } };
    var tech2 = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech Two" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Client A" };

    context.Technicians.AddRange(tech1, tech2);
    context.Customers.Add(customer);
    context.Jobs.AddRange(
        new Job { Id = Guid.NewGuid(), Status = "Completed", TechnicianId = tech1.Id, CustomerId = customer.Id },
        new Job { Id = Guid.NewGuid(), Status = "Completed", TechnicianId = tech1.Id, CustomerId = customer.Id },
        new Job { Id = Guid.NewGuid(), Status = "Completed", TechnicianId = tech2.Id, CustomerId = customer.Id }
    );

    await context.SaveChangesAsync();

    var controller = new DashboardController(context);
    var result = await controller.GetTechnicianActivity() as OkObjectResult;
    result.ShouldNotBeNull();

    dynamic list = result.Value!;
    list.ShouldContain(x => x.TechnicianName == "Tech One" && x.JobsCompleted == 2);
    list.ShouldContain(x => x.TechnicianName == "Tech Two" && x.JobsCompleted == 1);
  }
}