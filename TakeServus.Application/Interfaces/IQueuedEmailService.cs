using System.Threading.Tasks;

namespace TakeServus.Application.Interfaces;

public interface IQueuedEmailService
{
  Task EnqueueEmailAsync(string to, string subject, string body);
  Task ProcessPendingEmailsAsync();
}