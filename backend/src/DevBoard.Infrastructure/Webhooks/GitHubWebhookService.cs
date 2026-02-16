using System.Text.Json;
using DevBoard.Application.Realtime;
using DevBoard.Application.Webhooks.Services;
using DevBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskEntity = DevBoard.Domain.Entities.Task;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;

namespace DevBoard.Infrastructure.Webhooks;

public sealed class GitHubWebhookService(
    ApplicationDbContext dbContext,
    GitHubSignatureValidator signatureValidator,
    ITaskRealtimeNotifier realtimeNotifier,
    ILogger<GitHubWebhookService> logger) : IGitHubWebhookService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ProcessAsync(
        string eventName,
        string deliveryId,
        string signature,
        string payload,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(eventName, "issues", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        signatureValidator.Validate(payload, signature);

        var alreadyProcessed = await dbContext.WebhookDeliveries
            .AsNoTracking()
            .AnyAsync(item => item.DeliveryId == deliveryId, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var webhookPayload = JsonSerializer.Deserialize<GitHubIssueWebhookPayload>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid GitHub webhook payload.");

        if (webhookPayload.Issue is null || webhookPayload.Repository is null || webhookPayload.Repository.Owner is null)
        {
            throw new InvalidOperationException("GitHub webhook payload is missing issue or repository data.");
        }

        var repoOwner = webhookPayload.Repository.Owner.Login?.Trim();
        var repoName = webhookPayload.Repository.Name?.Trim();
        var issueNumber = webhookPayload.Issue.Number;

        if (string.IsNullOrWhiteSpace(repoOwner) || string.IsNullOrWhiteSpace(repoName) || issueNumber <= 0)
        {
            throw new InvalidOperationException("GitHub webhook payload contains invalid repository or issue values.");
        }

        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .FirstOrDefaultAsync(
                item => item.GitHubIssueNumber == issueNumber
                    && item.Project.RepoOwner == repoOwner
                    && item.Project.RepoName == repoName,
                cancellationToken);

        var hasChanges = false;
        if (task is not null)
        {
            hasChanges = ApplyWebhookAction(task, webhookPayload.Action, webhookPayload.Issue.Title);
        }
        else
        {
            logger.LogInformation(
                "No local task matched GitHub issue #{IssueNumber} in {Owner}/{Repo}.",
                issueNumber,
                repoOwner,
                repoName);
        }

        var delivery = new DevBoard.Domain.Entities.WebhookDelivery(deliveryId, eventName);
        dbContext.WebhookDeliveries.Add(delivery);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (task is null || !hasChanges)
        {
            return;
        }

        var taskUpdatedEvent = new TaskUpdatedEvent(
            task.Id,
            task.ProjectId,
            task.Status.ToString(),
            task.CompletedAt,
            "webhook");

        await realtimeNotifier.NotifyTaskUpdatedAsync(taskUpdatedEvent, cancellationToken);
    }

    private static bool ApplyWebhookAction(TaskEntity task, string? action, string? issueTitle)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        switch (action.Trim().ToLowerInvariant())
        {
            case "closed":
                task.ApplyGitHubStatus(TaskStatus.Done);
                return true;
            case "reopened":
                task.ApplyGitHubStatus(TaskStatus.InProgress);
                return true;
            case "edited":
                if (string.IsNullOrWhiteSpace(issueTitle))
                {
                    return false;
                }

                if (string.Equals(task.Title, issueTitle.Trim(), StringComparison.Ordinal))
                {
                    return false;
                }

                task.SyncTitleFromGitHub(issueTitle);
                return true;
            default:
                return false;
        }
    }

    private sealed class GitHubIssueWebhookPayload
    {
        public string? Action { get; init; }
        public GitHubIssuePayload? Issue { get; init; }
        public GitHubRepositoryPayload? Repository { get; init; }
    }

    private sealed class GitHubIssuePayload
    {
        public int Number { get; init; }
        public string? Title { get; init; }
    }

    private sealed class GitHubRepositoryPayload
    {
        public string? Name { get; init; }
        public GitHubOwnerPayload? Owner { get; init; }
    }

    private sealed class GitHubOwnerPayload
    {
        public string? Login { get; init; }
    }
}
