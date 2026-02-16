namespace DevBoard.Application.Realtime;

public sealed record TaskUpdatedEvent(
    Guid TaskId,
    Guid ProjectId,
    string Status,
    DateTime? CompletedAt,
    string UpdatedFrom);
