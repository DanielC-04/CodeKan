using DevBoard.Application.Common.Exceptions;
using DevBoard.Application.Tasks.Dtos;
using DevBoard.Application.Tasks.Services;
using Octokit;

namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubIssueService : IGitHubIssueService
{
    public async Task<int> CreateIssueAsync(
        string repoOwner,
        string repoName,
        string title,
        string? description,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = CreateClient(token);
            var issueRequest = new NewIssue(title)
            {
                Body = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
            };
            var createdIssue = await client.Issue.Create(repoOwner, repoName, issueRequest);
            return createdIssue.Number;
        }
        catch (AuthorizationException exception)
        {
            throw new GitHubIntegrationException("GitHub authorization failed. Verify token permissions.", exception);
        }
        catch (NotFoundException exception)
        {
            throw new GitHubIntegrationException("GitHub repository was not found.", exception);
        }
        catch (ApiException exception)
        {
            throw new GitHubIntegrationException($"GitHub API error while creating issue: {exception.Message}", exception);
        }
    }

    public async Task CloseIssueAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = CreateClient(token);
            var issueUpdate = new IssueUpdate { State = ItemState.Closed };
            await client.Issue.Update(repoOwner, repoName, issueNumber, issueUpdate);
        }
        catch (AuthorizationException exception)
        {
            throw new GitHubIntegrationException("GitHub authorization failed. Verify token permissions.", exception);
        }
        catch (NotFoundException exception)
        {
            throw new GitHubIntegrationException("GitHub issue or repository was not found.", exception);
        }
        catch (ApiException exception)
        {
            throw new GitHubIntegrationException($"GitHub API error while closing issue: {exception.Message}", exception);
        }
    }

    public async Task ReopenIssueAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = CreateClient(token);
            var issueUpdate = new IssueUpdate { State = ItemState.Open };
            await client.Issue.Update(repoOwner, repoName, issueNumber, issueUpdate);
        }
        catch (AuthorizationException exception)
        {
            throw new GitHubIntegrationException("GitHub authorization failed. Verify token permissions.", exception);
        }
        catch (NotFoundException exception)
        {
            throw new GitHubIntegrationException("GitHub issue or repository was not found.", exception);
        }
        catch (ApiException exception)
        {
            throw new GitHubIntegrationException($"GitHub API error while reopening issue: {exception.Message}", exception);
        }
    }

    public async Task<GitHubIssueDetailsDto> GetIssueDetailsAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = CreateClient(token);
            var issue = await client.Issue.Get(repoOwner, repoName, issueNumber);

            var assignees = issue.Assignees
                .Select(MapUser)
                .Where(item => item is not null)
                .Cast<GitHubIssueUserDto>()
                .ToList();

            var labels = issue.Labels
                .Select(label => new GitHubIssueLabelDto(label.Name, label.Color))
                .ToList();

            if (labels.Count == 0)
            {
                var explicitLabels = await client.Issue.Labels.GetAllForIssue(repoOwner, repoName, issueNumber);
                labels = explicitLabels
                    .Select(label => new GitHubIssueLabelDto(label.Name, label.Color))
                    .ToList();
            }

            return new GitHubIssueDetailsDto(
                Guid.Empty,
                issue.Number,
                issue.Title,
                issue.Body,
                issue.State.StringValue,
                issue.StateReason?.StringValue,
                MapUser(issue.User),
                assignees,
                labels,
                issue.Comments,
                issue.CreatedAt,
                issue.UpdatedAt ?? issue.CreatedAt,
                issue.HtmlUrl);
        }
        catch (AuthorizationException exception)
        {
            throw new GitHubIntegrationException("GitHub authorization failed. Verify token permissions.", exception);
        }
        catch (NotFoundException exception)
        {
            throw new GitHubIntegrationException("GitHub issue or repository was not found.", exception);
        }
        catch (ApiException exception)
        {
            throw new GitHubIntegrationException($"GitHub API error while reading issue details: {exception.Message}", exception);
        }
    }

    public async Task<IReadOnlyList<GitHubIssueCommentDto>> GetIssueCommentsAsync(
        string repoOwner,
        string repoName,
        int issueNumber,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var client = CreateClient(token);
            var comments = await client.Issue.Comment.GetAllForIssue(repoOwner, repoName, issueNumber);

            return comments
                .Select(comment => new GitHubIssueCommentDto(
                    comment.Id,
                    comment.Body,
                    MapUser(comment.User),
                    comment.CreatedAt,
                    comment.UpdatedAt ?? comment.CreatedAt,
                    comment.HtmlUrl))
                .ToList();
        }
        catch (AuthorizationException exception)
        {
            throw new GitHubIntegrationException("GitHub authorization failed. Verify token permissions.", exception);
        }
        catch (NotFoundException exception)
        {
            throw new GitHubIntegrationException("GitHub issue or repository was not found.", exception);
        }
        catch (ApiException exception)
        {
            throw new GitHubIntegrationException($"GitHub API error while reading issue comments: {exception.Message}", exception);
        }
    }

    private static GitHubIssueUserDto? MapUser(User? user)
    {
        if (user is null)
        {
            return null;
        }

        return new GitHubIssueUserDto(user.Login, user.AvatarUrl, user.HtmlUrl);
    }

    private static GitHubClient CreateClient(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new GitHubIntegrationException("GitHub token is required.");
        }

        var client = new GitHubClient(new ProductHeaderValue("DevBoard"))
        {
            Credentials = new Credentials(token)
        };

        return client;
    }
}
