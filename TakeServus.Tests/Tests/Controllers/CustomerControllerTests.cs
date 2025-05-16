using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Customers;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.UnitTests.Controllers;

public class CustomerControllerTests
{
  [Fact]
  public async Task CreateCustomer_Should_SaveSuccessfully()
  {
    var context = TestDbContextFactory.Create();
    var controller = new CustomerController(context);

    var request = new CreateCustomerRequest
    {
      FullName = "John Doe",
      Phone = "1234567890",
      Email = "john@example.com",
      Address = "123 Main St"
    };

    var result = await controller.CreateCustomer(request) as OkObjectResult;
    result.ShouldNotBeNull();

    var customerId = (Guid)result.Value!;
    customerId.ShouldNotBe(Guid.Empty);

    var customer = await context.Customers.FindAsync(customerId);
    customer.ShouldNotBeNull();
    customer.Email.ShouldBe("john@example.com");
  }

  [Fact]
  public async Task UpdateCustomer_Should_ModifyCustomer()
  {
    var context = TestDbContextFactory.Create();

    var customer = new Customer { Id = Guid.NewGuid(), FullName = "Old Name", Email = "old@example.com" };
    context.Customers.Add(customer);
    await context.SaveChangesAsync();

    var controller = new CustomerController(context);
    var updateRequest = new UpdateCustomerRequest
    {
      Id = customer.Id,
      FullName = "New Name",
      Phone = "9876543210",
      Email = "new@example.com",
      Address = "456 Elm St"
    };

    var result = await controller.UpdateCustomer(updateRequest) as OkResult;
    result.ShouldNotBeNull();

    var updated = await context.Customers.FindAsync(customer.Id);
    updated!.FullName.ShouldBe("New Name");
    updated.Email.ShouldBe("new@example.com");
  }

  [Fact]
  public async Task DeleteCustomer_Should_RemoveFromDb()
  {
    var context = TestDbContextFactory.Create();
    var customer = new Customer { Id = Guid.NewGuid(), FullName = "To Delete" };
    context.Customers.Add(customer);
    await context.SaveChangesAsync();

    var controller = new CustomerController(context);
    var result = await controller.DeleteCustomer(customer.Id) as OkResult;
    result.ShouldNotBeNull();

    var deleted = await context.Customers.FindAsync(customer.Id);
    deleted.ShouldBeNull();
  }

  [Fact]
  public async Task SearchCustomers_Should_ReturnMatches()
  {
    var context = TestDbContextFactory.Create();
    context.Customers.AddRange(
        new Customer { Id = Guid.NewGuid(), FullName = "Alice Smith", Email = "alice@example.com" },
        new Customer { Id = Guid.NewGuid(), FullName = "Bob Johnson", Email = "bob@example.com" }
    );
    await context.SaveChangesAsync();

    var controller = new CustomerController(context);
    var result = await controller.SearchCustomers("bob") as OkObjectResult;
    result.ShouldNotBeNull();

    var list = result.Value as System.Collections.Generic.List<CustomerResponse>;
    list.ShouldNotBeNull();
    list.Count.ShouldBe(1);
    list[0].FullName.ShouldContain("Bob");
  }
}
