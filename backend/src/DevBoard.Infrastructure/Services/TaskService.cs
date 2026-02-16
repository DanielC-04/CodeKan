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
    ITokenProtector tokenProtector,
    IGitHubIssueService gitHubIssueService,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        var token = tokenProtector.Unprotect(project.GitHubTokenEncrypted);
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

    public async Task<IReadOnlyList<TaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.CreatedAt)
            .Select(task => Map(task))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

        return task is null ? null : Map(task);
    }

    public async Task<TaskDto?> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);

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
            var project = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == task.ProjectId, cancellationToken);

            if (project is null)
            {
                throw new InvalidOperationException("Project not found for task.");
            }

            if (task.GitHubIssueNumber is null)
            {
                throw new InvalidOperationException("Task does not have an associated GitHub issue.");
            }

            var token = tokenProtector.Unprotect(project.GitHubTokenEncrypted);

            if (newStatus == TaskStatus.Done)
            {
                await gitHubIssueService.CloseIssueAsync(
                    project.RepoOwner,
                    project.RepoName,
                    task.GitHubIssueNumber.Value,
                    token,
                    cancellationToken);
            }
            else
            {
                await gitHubIssueService.ReopenIssueAsync(
                    project.RepoOwner,
                    project.RepoName,
                    task.GitHubIssueNumber.Value,
                    token,
                    cancellationToken);
            }
        }

        task.MoveTo(newStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    public async Task<GitHubIssueDetailsDto?> GetIssueDetailsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (task.GitHubIssueNumber is null)
        {
            throw new InvalidOperationException("Task does not have an associated GitHub issue.");
        }

        var token = tokenProtector.Unprotect(task.Project.GitHubTokenEncrypted);
        var details = await gitHubIssueService.GetIssueDetailsAsync(
            task.Project.RepoOwner,
            task.Project.RepoName,
            task.GitHubIssueNumber.Value,
            token,
            cancellationToken);

        return details with { TaskId = task.Id };
    }

    public async Task<IReadOnlyList<GitHubIssueCommentDto>?> GetIssueCommentsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks
            .Include(item => item.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (task.GitHubIssueNumber is null)
        {
            throw new InvalidOperationException("Task does not have an associated GitHub issue.");
        }

        var token = tokenProtector.Unprotect(task.Project.GitHubTokenEncrypted);
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
