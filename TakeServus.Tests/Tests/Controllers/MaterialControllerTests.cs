using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Materials;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.Tests.Controllers;

public class MaterialControllerTests
{
  [Fact]
  public async Task RefillMaterial_Should_UpdateStockCorrectly()
  {
    var context = TestDbContextFactory.Create();

    var material = new Material
    {
      Id = Guid.NewGuid(),
      Name = "Copper Wire",
      StockQuantity = 3
    };

    context.Materials.Add(material);
    await context.SaveChangesAsync();

    var controller = new MaterialController(context);
    var request = new RefillMaterialRequest
    {
      MaterialId = material.Id,
      QuantityToAdd = 7
    };

    var result = await controller.Refill(request) as OkResult;
    result.ShouldNotBeNull();

    var updated = await context.Materials.FindAsync(material.Id);
    updated!.StockQuantity.ShouldBe(10);
  }

  [Fact]
  public async Task RefillMaterial_WithInvalidId_ShouldReturnNotFound()
  {
    var context = TestDbContextFactory.Create();
    var controller = new MaterialController(context);

    var result = await controller.Refill(new RefillMaterialRequest
    {
      MaterialId = Guid.NewGuid(),
      QuantityToAdd = 5
    });

    result.ShouldBeOfType<NotFoundObjectResult>();
  }
}
