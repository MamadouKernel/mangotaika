using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
