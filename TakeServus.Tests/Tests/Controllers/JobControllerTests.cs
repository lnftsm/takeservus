using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TakeServus.Tests.Tests.Controllers;

public class JobControllerTests
{
  private TakeServusDbContext GetInMemoryDbContext()
  {
    var options = new DbContextOptionsBuilder<TakeServusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new TakeServusDbContext(options);
  }

  private static ClaimsPrincipal GetFakeUser(string userId)
  {
    return new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));
  }

  [Fact]
  public async Task CreateJob_ShouldAddJobAndActivity()
  {
    // Arrange
    var dbContext = GetInMemoryDbContext();
    var mockEmailService = new Mock<IEmailService>();

    var controller = new JobController(dbContext, mockEmailService.Object)
    {
      ControllerContext = new ControllerContext
      {
        HttpContext = new DefaultHttpContext
        {
          User = GetFakeUser(Guid.NewGuid().ToString())
        }
      }
    };

    var technician = new Technician
    {
      Id = Guid.NewGuid(),
      User = new User
      {
        Id = Guid.NewGuid(),
        Email = "tech@example.com",
        FullName = "Test Tech",
        Role = "Technician",
        IsActive = true
      },
      UserId = Guid.NewGuid()
    };

    var customer = new Customer
    {
      Id = Guid.NewGuid(),
      FullName = "Customer Test",
      Email = "customer@example.com"
    };

    dbContext.Technicians.Add(technician);
    dbContext.Customers.Add(customer);
    await dbContext.SaveChangesAsync();

    var request = new CreateJobRequest
    {
      CustomerId = customer.Id,
      TechnicianId = technician.Id,
      Title = "Test Job",
      Description = "Testing create",
      ScheduledAt = DateTime.UtcNow.AddHours(1)
    };

    // Act
    var result = await controller.CreateJob(request);

    // Assert
    var jobs = await dbContext.Jobs.ToListAsync();
    var activities = await dbContext.JobActivities.ToListAsync();

    jobs.Should().HaveCount(1);
    activities.Should().ContainSingle(a => a.ActivityType == "JobCreated");

    result.Should().BeOfType<OkObjectResult>();
  }
}
