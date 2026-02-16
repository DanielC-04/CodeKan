namespace DevBoard.Application.Realtime;

public interface ITaskRealtimeNotifier
{
    Task NotifyTaskUpdatedAsync(TaskUpdatedEvent taskUpdatedEvent, CancellationToken cancellationToken = default);
}
