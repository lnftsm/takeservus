using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public class JobControllerTests
{
  [Fact]
  public async Task CreateJob_Should_Create_Job_And_Send_Email()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<TakeServusDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    using var context = new TakeServusDbContext(options);
    var mockEmail = new Mock<IQueuedEmailService>();
    var mockStorage = new Mock<IFileStorageService>();

    var technician = new Technician
    {
      Id = Guid.NewGuid(),
      User = new User { Id = Guid.NewGuid(), Email = "tech@example.com", FullName = "Tech Name" }
    };

    context.Technicians.Add(technician);
    context.SaveChanges();

    var controller = new JobController(context, mockEmail.Object, mockStorage.Object);
    var request = new CreateJobRequest
    {
      CustomerId = Guid.NewGuid(),
      TechnicianId = technician.Id,
      Title = "Fix A/C",
      Description = "AC not cooling",
      ScheduledAt = DateTime.UtcNow.AddDays(1)
    };

    // Act
    var result = await controller.CreateJob(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(okResult.Value);
    mockEmail.Verify(e => e.EnqueueEmailAsync(
        technician.User.Email,
        It.IsAny<string>(),
        It.IsAny<string>()),
        Times.Once
    );
  }
  [Fact]
  public async Task ReassignJob_Should_ReassignSuccessfully()
  {
    [Fact]
    public async Task ReassignJob_Should_ReassignSuccessfully()
    {
      var context = TestDbContextFactory.Create();

      var technician1 = new Technician
      {
        Id = Guid.NewGuid(),
        User = new User { Id = Guid.NewGuid(), FullName = "Tech One" }
      };
      var technician2 = new Technician
      {
        Id = Guid.NewGuid(),
        User = new User { Id = Guid.NewGuid(), FullName = "Tech Two" }
      };
      var customer = new Customer { Id = Guid.NewGuid(), FullName = "John Customer" };
      var job = new Job
      {
        Id = Guid.NewGuid(),
        TechnicianId = technician1.Id,
        CustomerId = customer.Id,
        Title = "Initial Job",
        Status = "Scheduled",
        ScheduledAt = DateTime.UtcNow
      };

      context.Customers.Add(customer);
      context.Technicians.AddRange(technician1, technician2);
      context.Jobs.Add(job);
      await context.SaveChangesAsync();

      var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());
      var request = new ReassignJobRequest
      {
        JobId = job.Id,
        NewTechnicianId = technician2.Id
      };

      var result = await controller.ReassignJob(request) as OkObjectResult;

      result.ShouldNotBeNull();
      var updatedJob = await context.Jobs.FindAsync(job.Id);
      updatedJob.TechnicianId.ShouldBe(technician2.Id);
    }
  }

  [Fact]
  public async Task UpdateStatus_Should_UpdateStatusSuccessfully()
  {
    var context = TestDbContextFactory.Create();

    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech 1" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Jane Customer" };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Job Test",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());
    var request = new UpdateJobStatusRequest
    {
      JobId = job.Id,
      Status = "Started"
    };

    var result = await controller.UpdateStatus(request) as OkResult;

    result.ShouldNotBeNull();
    var updated = await context.Jobs.FindAsync(job.Id);
    updated!.Status.ShouldBe("Started");
    updated.StartedAt.ShouldNotBeNull();
  }

  [Fact]
  public async Task AddNote_Should_AddNoteSuccessfully()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer X" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech X" } };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Job with Notes",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());
    var request = new CreateJobNoteRequest
    {
      JobId = job.Id,
      Note = "Test note added"
    };

    var result = await controller.AddNote(request) as OkObjectResult;

    result.ShouldNotBeNull();
    var noteId = (Guid)result.Value!;
    noteId.ShouldNotBe(Guid.Empty);

    var note = await context.JobNotes.FindAsync(noteId);
    note.ShouldNotBeNull();
    note.Note.ShouldBe("Test note added");
  }

  [Fact]
  public async Task AddMaterial_Should_AssignMaterialSuccessfully()
  {
    var context = TestDbContextFactory.Create();

    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech Y" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer Y" };
    var material = new Material { Id = Guid.NewGuid(), Name = "Pipe", StockQuantity = 5 };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Material Job",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Materials.Add(material);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());

    var request = new JobAssignMaterialRequest
    {
      JobId = job.Id,
      MaterialId = material.Id,
      QuantityUsed = 2
    };

    var result = await controller.AddMaterial(request) as OkResult;

    result.ShouldNotBeNull();
    var jobMaterial = await context.JobMaterials.FirstOrDefaultAsync(jm => jm.JobId == job.Id && jm.MaterialId == material.Id);
    jobMaterial.ShouldNotBeNull();
    jobMaterial.QuantityUsed.ShouldBe(2);

    var updatedMaterial = await context.Materials.FindAsync(material.Id);
    updatedMaterial!.StockQuantity.ShouldBe(3);
  }

  [Fact]
  public async Task UploadPhoto_Should_UploadPhotoSuccessfully()
  {
    var context = TestDbContextFactory.Create();

    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech Z" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer Z" };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Photo Job",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow
    };

    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var mockStorage = new Mock<IFileStorageService>();
    mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
               .ReturnsAsync("/mock/path/photo.jpg");

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), mockStorage.Object);

    var fileMock = new Mock<IFormFile>();
    var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("FakeImageContent"));
    fileMock.Setup(_ => _.OpenReadStream()).Returns(content);
    fileMock.Setup(_ => _.FileName).Returns("test.jpg");
    fileMock.Setup(_ => _.Length).Returns(content.Length);

    var result = await controller.UploadPhoto(job.Id, fileMock.Object) as OkObjectResult;

    result.ShouldNotBeNull();
    var value = result.Value as dynamic;
    ((string)value!.PhotoUrl).ShouldBe("/mock/path/photo.jpg");

    var saved = await context.JobPhotos.FirstOrDefaultAsync(p => p.JobId == job.Id);
    saved.ShouldNotBeNull();
    saved.PhotoUrl.ShouldBe("/mock/path/photo.jpg");
  }

  [Fact]
  public async Task GetMyJobs_Should_ReturnFilteredResults()
  {
    var context = TestDbContextFactory.Create();

    var userId = Guid.NewGuid();
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = userId, FullName = "Tech Filter" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer F" };

    context.Users.Add(technician.User);
    context.Technicians.Add(technician);
    context.Customers.Add(customer);

    var job1 = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Scheduled Job",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow.Date.AddHours(9)
    };

    var job2 = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Completed Job",
      Status = "Completed",
      ScheduledAt = DateTime.UtcNow.Date.AddDays(-1)
    };

    context.Jobs.AddRange(job1, job2);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());
    var httpContext = new DefaultHttpContext();
    httpContext.User = new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(new[]
        {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Technician")
        }, "mock"));

    controller.ControllerContext = new ControllerContext
    {
      HttpContext = httpContext
    };

    var result = await controller.GetMyJobs("Scheduled", null) as OkObjectResult;
    result.ShouldNotBeNull();

    var list = result.Value as List<JobResponse>;
    list.ShouldNotBeNull();
    list.Count.ShouldBe(1);
    list[0].Title.ShouldBe("Scheduled Job");
  }

  [Fact]
  public async Task GetActivity_Should_ReturnJobActivities()
  {
    var context = TestDbContextFactory.Create();

    var user = new User { Id = Guid.NewGuid(), FullName = "Admin Viewer" };
    var technician = new Technician { Id = Guid.NewGuid(), User = user };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer A" };
    var job = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Test Job",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow
    };
    var activity1 = new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = job.Id,
      ActivityType = "Created",
      Details = "Job created",
      PerformedByUserId = user.Id,
      PerformedAt = DateTime.UtcNow.AddMinutes(-10),
      PerformedByUser = user
    };
    var activity2 = new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = job.Id,
      ActivityType = "Updated",
      Details = "Job updated",
      PerformedByUserId = user.Id,
      PerformedAt = DateTime.UtcNow,
      PerformedByUser = user
    };

    context.Users.Add(user);
    context.Customers.Add(customer);
    context.Technicians.Add(technician);
    context.Jobs.Add(job);
    context.JobActivities.AddRange(activity1, activity2);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());

    var result = await controller.GetActivity(job.Id) as OkObjectResult;
    result.ShouldNotBeNull();

    var list = result.Value as List<JobActivityResponse>;
    list.ShouldNotBeNull();
    list.Count.ShouldBe(2);
    list[0].ActivityType.ShouldBe("Updated");
    list[1].ActivityType.ShouldBe("Created");
  }

  [Fact]
  public async Task GetAllJobs_Should_ReturnPaginatedAndFilteredList()
  {
    var context = TestDbContextFactory.Create();

    var dispatcherUserId = Guid.NewGuid();
    var dispatcher = new User { Id = dispatcherUserId, FullName = "Dispatcher", Role = "Dispatcher" };
    var technician = new Technician { Id = Guid.NewGuid(), User = new User { Id = Guid.NewGuid(), FullName = "Tech A" } };
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Customer B" };

    context.Users.Add(dispatcher);
    context.Technicians.Add(technician);
    context.Customers.Add(customer);

    var job1 = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Fix Pipe",
      Status = "Scheduled",
      ScheduledAt = DateTime.UtcNow.Date.AddHours(10)
    };

    var job2 = new Job
    {
      Id = Guid.NewGuid(),
      TechnicianId = technician.Id,
      CustomerId = customer.Id,
      Title = "Repair Leak",
      Status = "Completed",
      ScheduledAt = DateTime.UtcNow.Date.AddDays(-2)
    };

    context.Jobs.AddRange(job1, job2);
    await context.SaveChangesAsync();

    var controller = new JobController(context, Mock.Of<IQueuedEmailService>(), Mock.Of<IFileStorageService>());
    var httpContext = new DefaultHttpContext();
    httpContext.User = new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(new[]
        {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, dispatcherUserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Dispatcher")
        }, "mock"));

    controller.ControllerContext = new ControllerContext
    {
      HttpContext = httpContext
    };

    var result = await controller.GetAllJobs(null, null, null, null, null, false, 1, 10) as OkObjectResult;
    result.ShouldNotBeNull();

    var paged = result.Value as PagedResult<JobResponse>;
    paged.ShouldNotBeNull();
    paged.Items.Count.ShouldBe(2);
    paged.TotalCount.ShouldBe(2);
  }
}