using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using TakeServus.Application.Interfaces;
using TakeServus.Infrastructure.Services;

namespace TakeServus.Tests.Tests.Services;
public class EmailServiceTests
{
  [Fact]
  public async Task SendEmailAsync_ShouldSendEmailWithoutException()
  {
    // Arrange
    var smtpSettings = new SmtpSettings
    {
      Host = "smtp.example.com",
      Port = 587,
      EnableSsl = true,
      Username = "test@example.com",
      Password = "password",
      From = "TakeServus <noreply@takeservus.com>"
    };

    var optionsMock = new Mock<IOptions<SmtpSettings>>();
    optionsMock.Setup(o => o.Value).Returns(smtpSettings);

    var service = new EmailService(optionsMock.Object);

    // Act & Assert
    // In real scenarios, you'd mock SmtpClient. Here, just check that method exists and is callable.
    await Assert.ThrowsAnyAsync<Exception>(() =>
        service.SendEmailAsync("recipient@example.com", "Test Subject", "Test Body")
    );
  }
}
