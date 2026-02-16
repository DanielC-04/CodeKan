namespace DevBoard.Application.Projects.Dtos;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string RepoOwner,
    string RepoName,
    DateTime CreatedAt);
