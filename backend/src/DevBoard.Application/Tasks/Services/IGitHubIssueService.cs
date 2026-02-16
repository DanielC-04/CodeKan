using DevBoard.Application.Tasks.Dtos;

namespace DevBoard.Application.Tasks.Services;

public interface IGitHubIssueService
{
    Task<int> CreateIssueAsync(
        string repoOwner,
        string repoName,
        string title,
        string? description,
        string token,
        CancellationToken cancellationToken = default);

    Task CloseIssueAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default);

    Task ReopenIssueAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default);

    Task<GitHubIssueDetailsDto> GetIssueDetailsAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitHubIssueCommentDto>> GetIssueCommentsAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default);
}
