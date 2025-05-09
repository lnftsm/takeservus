using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TakeServus.Api.Controllers;
using TakeServus.Application.DTOs.Auth;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using Xunit;

namespace TakeServus.Tests.Controllers;

public class AuthControllerTests
{
  [Fact]
  public async Task Login_With_ValidCredentials_Should_ReturnToken()
  {
    var context = TestDbContextFactory.Create();

    var user = new User
    {
      Id = Guid.NewGuid(),
      FullName = "Auth Tester",
      Email = "auth@test.com",
      HashedPassword = BCrypt.Net.BCrypt.HashPassword("Test1234"),
      Role = "Dispatcher"
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var controller = new AuthController(context, JwtTestFactory.CreateJwtSettings());

    var request = new LoginRequest
    {
      Email = "auth@test.com",
      Password = "Test1234"
    };

    var result = await controller.Login(request) as OkObjectResult;
    result.ShouldNotBeNull();

    var token = result.Value as string;
    token.ShouldNotBeNullOrWhiteSpace();
  }

  [Fact]
  public async Task Login_With_InvalidCredentials_Should_ReturnUnauthorized()
  {
    var context = TestDbContextFactory.Create();

    var user = new User
    {
      Id = Guid.NewGuid(),
      FullName = "Auth Tester",
      Email = "auth@wrong.com",
      HashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
      Role = "Technician"
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var controller = new AuthController(context, JwtTestFactory.CreateJwtSettings());

    var request = new LoginRequest
    {
      Email = "auth@wrong.com",
      Password = "WrongPassword"
    };

    var result = await controller.Login(request);
    result.ShouldBeOfType<UnauthorizedResult>();
  }
}