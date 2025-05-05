using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TakeServus.Api.Middleware;

public class RequestLoggingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<RequestLoggingMiddleware> _logger;

  public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var stopwatch = Stopwatch.StartNew();

    await _next(context);

    stopwatch.Stop();

    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
    var method = context.Request.Method;
    var path = context.Request.Path;
    var statusCode = context.Response.StatusCode;
    var elapsed = stopwatch.ElapsedMilliseconds;

    _logger.LogInformation("{Timestamp} [{Method}] {Path} responded {StatusCode} in {Elapsed}ms by User {UserId}",
        DateTime.UtcNow, method, path, statusCode, elapsed, userId);
  }
}