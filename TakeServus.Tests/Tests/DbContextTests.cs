using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;
using FluentAssertions;

namespace TakeServus.Tests.Tests;
public class DbContextTests
{
  private TakeServusDbContext GetDbContext()
  {
    var options = new DbContextOptionsBuilder<TakeServusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new TakeServusDbContext(options);
  }

  [Fact]
  public async Task Should_Insert_And_Retrieve_JobEntity()
  {
    // Arrange
    var context = GetDbContext();

    var job = new Job
    {
      Id = Guid.NewGuid(),
      Title = "Test Job",
      Description = "Test Desc",
      Status = "Scheduled",
      CreatedAt = DateTime.UtcNow
    };

    // Act
    context.Jobs.Add(job);
    await context.SaveChangesAsync();

    var retrieved = await context.Jobs.FirstOrDefaultAsync(j => j.Id == job.Id);

    // Assert
    retrieved.Should().NotBeNull();
    retrieved!.Title.Should().Be("Test Job");
  }
}