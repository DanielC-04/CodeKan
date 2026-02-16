namespace DevBoard.Application.Tasks.Dtos;

public sealed record GitHubIssueUserDto(
    string Login,
    string? AvatarUrl,
    string? ProfileUrl);

public sealed record GitHubIssueLabelDto(
    string Name,
    string? Color);

public sealed record GitHubIssueCommentDto(
    long Id,
    string Body,
    GitHubIssueUserDto? Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Url);

public sealed record GitHubIssueDetailsDto(
    Guid TaskId,
    int IssueNumber,
    string Title,
    string? Description,
    string State,
    string? StateReason,
    GitHubIssueUserDto? Author,
    IReadOnlyList<GitHubIssueUserDto> Assignees,
    IReadOnlyList<GitHubIssueLabelDto> Labels,
    int CommentsCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Url);
