namespace MangoTaika.Services;

public interface INotificationDispatchService
{
    Task SendAsync(IEnumerable<Guid> userIds, string title, string message, string category, string? link = null);
}
