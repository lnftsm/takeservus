using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TakeServus.Application.Interfaces;
using TakeServus.Application.Settings;

public class EmailService : IEmailService
{
  private readonly SmtpSettings _settings;

  public EmailService(IOptions<SmtpSettings> settings)
  {
    _settings = settings.Value;
  }

  public async Task SendEmailAsync(string to, string subject, string body)
  {
    using var client = new SmtpClient(_settings.Host, _settings.Port)
    {
      Credentials = new NetworkCredential(_settings.Username, _settings.Password),
      EnableSsl = _settings.EnableSsl
    };

    var mail = new MailMessage(_settings.From, to, subject, body)
    {
      IsBodyHtml = false
    };

    await client.SendMailAsync(mail);
  }
}