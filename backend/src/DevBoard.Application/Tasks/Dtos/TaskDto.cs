namespace DevBoard.Application.Tasks.Dtos;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Status,
    int? GitHubIssueNumber,
    DateTime CreatedAt,
    DateTime? CompletedAt);
