using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Tasks.Dtos;
using DevBoard.Application.Tasks.Services;
using DevBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;
using TaskEntity = DevBoard.Domain.Entities.Task;

namespace DevBoard.Infrastructure.Services;

public sealed class TaskService(
    ApplicationDbContext dbContext,
    IGitHubIssueService gitHubIssueService,
    IGitHubAppTokenService gitHubAppTokenService,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<TaskDto> CreateAsync(
        Guid ownerUserId,
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(item => item.Id == projectId && item.OwnerUserId == ownerUserId, cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        if (!project.GitHubInstallationId.HasValue)
        {
            throw new InvalidOperationException("GitHub App installation is not configured for this project.");
        }

        var token = await gitHubAppTokenService.GetInstallationTokenAsync(project.GitHubInstallationId.Value, cancellationToken);
        var createdIssueNumber = await gitHubIssueService.CreateIssueAsync(
            project.RepoOwner,
            project.RepoName,
            request.Title,
            request.Description,
            token,
            cancellationToken);

        var task = new TaskEntity(projectId, request.Title);
        task.SetGitHubIssueNumber(createdIssueNumber);
        dbContext.Tasks.Add(task);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist task after creating GitHub issue {IssueNumber}.", createdIssueNumber);

            try
            {
                await gitHubIssueService.CloseIssueAsync(
                    project.RepoOwner,
                    project.RepoName,
                    createdIssueNumber,
                    token,
                    cancellationToken);
            }
            catch (Exception compensationException)
            {
                logger.LogError(
                    compensationException,
                    "Compensation failed for GitHub issue {IssueNumber} after DB persistence error.",
                    createdIssueNumber);
            }

            throw;
        }

        return Map(task);
    }

    public async Task<IReadOnlyList<TaskDto>> GetByProjectIdAsync(
        Guid ownerUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId && task.Project.OwnerUserId == ownerUserId)
            .OrderBy(task => task.CreatedAt)
            .Select(task => Map(task))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDto?> GetByIdAsync(Guid ownerUserId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == taskId && task.Project.OwnerUserId == ownerUserId, cancellationToken);

        return task is null ? null : Map(task);
    }

    public async Task<TaskDto?> UpdateStatusAsync(
        Guid ownerUserId,
        Guid taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == taskId && item.Project.OwnerUserId == ownerUserId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (!Enum.TryParse<TaskStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            throw new InvalidOperationException("Invalid status value.");
        }

        var currentStatus = task.Status;

        if (currentStatus != newStatus && (newStatus == TaskStatus.Done || (currentStatus == TaskStatus.Done && newStatus == TaskStatus.InProgress)))
        {
            if (task.GitHubIssueNumber is null)
            {
                throw new InvalidOperationException("Task does not have an associated GitHub issue.");
            }

            if (!task.Project.GitHubInstallationId.HasValue)
            {
                throw new InvalidOperationException("GitHub App installation is not configured for this project.");
            }

            var token = await gitHubAppTokenService.GetInstallationTokenAsync(task.Project.GitHubInstallationId.Value, cancellationToken);

            if (newStatus == TaskStatus.Done)
            {
                await gitHubIssueService.CloseIssueAsync(
                    task.Project.RepoOwner,
                    task.Project.RepoName,
                    task.GitHubIssueNumber.Value,
                    token,
                    cancellationToken);
            }
            else
            {
                await gitHubIssueService.ReopenIssueAsync(
                    task.Project.RepoOwner,
                    task.Project.RepoName,
                    task.GitHubIssueNumber.Value,
                    token,
                    cancellationToken);
            }
        }

        task.MoveTo(newStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    public async Task<GitHubIssueDetailsDto?> GetIssueDetailsAsync(
        Guid ownerUserId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId && item.Project.OwnerUserId == ownerUserId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (task.GitHubIssueNumber is null)
        {
            throw new InvalidOperationException("Task does not have an associated GitHub issue.");
        }

        if (!task.Project.GitHubInstallationId.HasValue)
        {
            throw new InvalidOperationException("GitHub App installation is not configured for this project.");
        }

        var token = await gitHubAppTokenService.GetInstallationTokenAsync(task.Project.GitHubInstallationId.Value, cancellationToken);
        var details = await gitHubIssueService.GetIssueDetailsAsync(
            task.Project.RepoOwner,
            task.Project.RepoName,
            task.GitHubIssueNumber.Value,
            token,
            cancellationToken);

        return details with { TaskId = task.Id };
    }

    public async Task<IReadOnlyList<GitHubIssueCommentDto>?> GetIssueCommentsAsync(
        Guid ownerUserId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId && item.Project.OwnerUserId == ownerUserId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (task.GitHubIssueNumber is null)
        {
            throw new InvalidOperationException("Task does not have an associated GitHub issue.");
        }

        if (!task.Project.GitHubInstallationId.HasValue)
        {
            throw new InvalidOperationException("GitHub App installation is not configured for this project.");
        }

        var token = await gitHubAppTokenService.GetInstallationTokenAsync(task.Project.GitHubInstallationId.Value, cancellationToken);
        return await gitHubIssueService.GetIssueCommentsAsync(
            task.Project.RepoOwner,
            task.Project.RepoName,
            task.GitHubIssueNumber.Value,
            token,
            cancellationToken);
    }

    private static TaskDto Map(TaskEntity task) =>
        new(task.Id, task.ProjectId, task.Title, task.Status.ToString(), task.GitHubIssueNumber, task.CreatedAt, task.CompletedAt);
}
