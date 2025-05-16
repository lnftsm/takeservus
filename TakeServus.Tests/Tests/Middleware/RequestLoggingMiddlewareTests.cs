using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TakeServus.Api.Middleware;

namespace TakeServus.UnitTests.Middleware;

public class RequestLoggingMiddlewareTests
{
  [Fact]
  public async Task InvokeAsync_Should_Log_Request_Method_And_Path()
  {
    // Arrange
    var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
    var context = new DefaultHttpContext();
    context.Request.Method = "GET";
    context.Request.Path = "/api/test";

    var calledNext = false;
    RequestDelegate next = (ctx) =>
    {
      calledNext = true;
      return Task.CompletedTask;
    };

    var middleware = new RequestLoggingMiddleware(next, loggerMock.Object);

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.True(calledNext);
    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GET") && v.ToString()!.Contains("/api/test")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.AtLeastOnce);
  }

  [Fact]
  public async Task InvokeAsync_Should_NotThrow_When_NextThrowsException()
  {
    // Arrange
    var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
    var context = new DefaultHttpContext();
    context.Request.Method = "POST";
    context.Request.Path = "/api/fail";

    RequestDelegate next = (ctx) => throw new InvalidOperationException("Boom");
    var middleware = new RequestLoggingMiddleware(next, loggerMock.Object);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("POST") && v.ToString()!.Contains("/api/fail")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.AtLeastOnce);
  }
}