using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Feedback;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.UnitTests.Controllers;

public class FeedbackControllerTests
{
  [Fact]
  public async Task SubmitFeedback_Should_SaveSuccessfully()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Test Customer" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech F" } };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      CustomerId = customer.Id,
      TechnicianId = technician.Id,
      Title = "Fix Feedback",
      Status = "Completed",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var controller = new FeedbackController(context);
    var request = new CreateJobFeedbackRequest
    {
      JobId = job.Id,
      CustomerSatisfaction = true,
      TechnicianRating = 4,
      Comment = "Very good work"
    };

    var result = await controller.SubmitFeedback(request) as OkObjectResult;
    result.ShouldNotBeNull();

    var feedbackId = (Guid)result.Value!;
    feedbackId.ShouldNotBe(Guid.Empty);

    var feedback = await context.JobFeedbacks.FindAsync(feedbackId);
    feedback.ShouldNotBeNull();
    feedback.TechnicianRating.ShouldBe(4);
  }

  [Fact]
  public async Task GetFeedbackByJobId_Should_ReturnCorrectFeedback()
  {
    var context = TestDbContextFactory.Create();

    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech G" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer G" };
    var job = new Job { Id = Guid.NewGuid(), TechnicianId = technician.Id, CustomerId = customer.Id, Title = "Job G", Status = "Completed" };
    var feedback = new JobFeedback
    {
      Id = Guid.NewGuid(),
      JobId = job.Id,
      TechnicianRating = 5,
      CustomerSatisfaction = true,
      Comment = "Excellent",
      SubmittedAt = DateTime.UtcNow
    };

    context.Users.Add(technician.User);
    context.Technicians.Add(technician);
    context.Customers.Add(customer);
    context.Jobs.Add(job);
    context.JobFeedbacks.Add(feedback);
    await context.SaveChangesAsync();

    var controller = new FeedbackController(context);

    var result = await controller.GetFeedback(job.Id) as OkObjectResult;
    result.ShouldNotBeNull();

    var dto = result.Value as JobFeedbackResponse;
    dto.ShouldNotBeNull();
    dto.TechnicianRating.ShouldBe(5);
    dto.Comment.ShouldBe("Excellent");
  }

  [Fact]
  public async Task GetTechnicianAverageRatings_Should_CalculateCorrectly()
  {
    var context = TestDbContextFactory.Create();

    var tech1 = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech A" } };
    var tech2 = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech B" } };

    var job1 = new Job { Id = Guid.NewGuid(), TechnicianId = tech1.Id, CustomerId = Guid.NewGuid() };
    var job2 = new Job { Id = Guid.NewGuid(), TechnicianId = tech1.Id, CustomerId = Guid.NewGuid() };
    var job3 = new Job { Id = Guid.NewGuid(), TechnicianId = tech2.Id, CustomerId = Guid.NewGuid() };

    var feedbacks = new List<JobFeedback>
        {
            new() { Id = Guid.NewGuid(), JobId = job1.Id, TechnicianRating = 5 },
            new() { Id = Guid.NewGuid(), JobId = job2.Id, TechnicianRating = 3 },
            new() { Id = Guid.NewGuid(), JobId = job3.Id, TechnicianRating = 4 }
        };

    context.Users.AddRange(tech1.User, tech2.User);
    context.Technicians.AddRange(tech1, tech2);
    context.Jobs.AddRange(job1, job2, job3);
    context.JobFeedbacks.AddRange(feedbacks);
    await context.SaveChangesAsync();

    var controller = new AdminController(context);
    var result = await controller.GetTechnicianRatings() as OkObjectResult;
    result.ShouldNotBeNull();

    var list = result.Value as List<TechnicianRatingResponse>;
    list.ShouldNotBeNull();
    list.Count.ShouldBe(2);

    var tech1Avg = list.FirstOrDefault(x => x.TechnicianName == tech1.User.FullName);
    tech1Avg.ShouldNotBeNull();
    tech1Avg.AverageRating.ShouldBe(4);
  }
}