using DevBoard.Domain.Exceptions;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;

namespace DevBoard.Domain.Entities;

public sealed class Task
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; }
    public TaskStatus Status { get; private set; }
    public int? GitHubIssueNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Project Project { get; private set; } = null!;

    private Task()
    {
        Title = string.Empty;
    }

    public Task(Guid projectId, string title, DateTime? createdAt = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("projectId is required.");
        }

        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = ValidateTitle(title);
        Status = TaskStatus.Todo;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        if (Status == TaskStatus.Done)
        {
            throw new DomainException("Done tasks cannot be edited.");
        }

        Title = ValidateTitle(title);
    }

    public void SyncTitleFromGitHub(string title)
    {
        Title = ValidateTitle(title);
    }

    public void SetGitHubIssueNumber(int gitHubIssueNumber)
    {
        if (gitHubIssueNumber <= 0)
        {
            throw new DomainException("gitHubIssueNumber must be greater than zero.");
        }

        GitHubIssueNumber = gitHubIssueNumber;
    }

    public void MoveTo(TaskStatus newStatus, DateTime? completedAt = null)
    {
        if (Status == newStatus)
        {
            return;
        }

        if (Status == TaskStatus.Done && newStatus != TaskStatus.InProgress)
        {
            throw new DomainException("Done tasks cannot move to another status.");
        }

        if (newStatus is not (TaskStatus.InProgress or TaskStatus.Done))
        {
            throw new DomainException("Invalid task status transition.");
        }

        Status = newStatus;

        if (newStatus == TaskStatus.Done)
        {
            CompletedAt = completedAt ?? DateTime.UtcNow;
            return;
        }

        CompletedAt = null;
    }

    public void ApplyGitHubStatus(TaskStatus newStatus, DateTime? completedAt = null)
    {
        if (newStatus is not (TaskStatus.InProgress or TaskStatus.Done))
        {
            throw new DomainException("Invalid task status from GitHub.");
        }

        Status = newStatus;

        if (newStatus == TaskStatus.Done)
        {
            CompletedAt = completedAt ?? DateTime.UtcNow;
            return;
        }

        CompletedAt = null;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("title is required.");
        }

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > 250)
        {
            throw new DomainException("title must be 250 characters or fewer.");
        }

        return normalizedTitle;
    }
}
