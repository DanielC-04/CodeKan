using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevBoard.Api.Hubs;

[Authorize]
public sealed class DevBoardHub : Hub
{
}
