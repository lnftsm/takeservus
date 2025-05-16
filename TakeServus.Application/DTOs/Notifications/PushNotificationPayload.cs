namespace TakeServus.Application.DTOs.Notifications;

public class PushNotificationPayload
{
  public string Title { get; set; } = default!;
  public string Body { get; set; } = default!;
  public Guid? JobId { get; set; }
}