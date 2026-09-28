using Microsoft.AspNetCore.SignalR;

namespace Service.Hubs;

[Authorize]
public class ChatHub : Hub;
