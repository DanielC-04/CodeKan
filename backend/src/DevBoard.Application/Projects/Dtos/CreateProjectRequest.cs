namespace DevBoard.Application.Projects.Dtos;

public sealed record CreateProjectRequest(
    string Name,
    string RepoOwner,
    string RepoName,
    string GitHubToken);
