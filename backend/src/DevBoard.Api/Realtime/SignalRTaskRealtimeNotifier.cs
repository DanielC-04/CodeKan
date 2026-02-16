using DevBoard.Api.Hubs;
using DevBoard.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace DevBoard.Api.Realtime;

public sealed class SignalRTaskRealtimeNotifier(IHubContext<DevBoardHub> hubContext) : ITaskRealtimeNotifier
{
    public Task NotifyTaskUpdatedAsync(TaskUpdatedEvent taskUpdatedEvent, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.All.SendAsync("TaskUpdated", taskUpdatedEvent, cancellationToken);
    }
}
