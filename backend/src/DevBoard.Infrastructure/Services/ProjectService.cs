using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Projects.Dtos;
using DevBoard.Application.Projects.Services;
using DevBoard.Application.Tasks.Services;
using DevBoard.Domain.Entities;
using DomainTaskStatus = DevBoard.Domain.Enums.TaskStatus;
using DevBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevBoard.Infrastructure.Services;

public sealed class ProjectService(
    ApplicationDbContext dbContext,
    IGitHubIssueService gitHubIssueService,
    IGitHubAppTokenService gitHubAppTokenService,
    IGitHubAppInstallationService gitHubAppInstallationService) : IProjectService
{
    public async Task<ProjectDto> CreateAsync(Guid ownerUserId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = new Project(
            ownerUserId,
            request.Name,
            request.RepoOwner,
            request.RepoName,
            gitHubInstallationId: null);

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        var installationId = await gitHubAppInstallationService.GetInstallationIdForRepoAsync(
            project.RepoOwner,
            project.RepoName,
            cancellationToken);

        if (installationId.HasValue)
        {
            project.SetGitHubInstallation(installationId.Value);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Map(project);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.OwnerUserId == ownerUserId)
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => Map(project))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid ownerUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == projectId && project.OwnerUserId == ownerUserId, cancellationToken);

        return project is null ? null : Map(project);
    }

    public async Task<bool> IsGitHubInstallationConfiguredAsync(
        Guid ownerUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(item => item.Id == projectId && item.OwnerUserId == ownerUserId && item.GitHubInstallationId.HasValue, cancellationToken);
    }

    public async Task<ProjectDto?> ConnectGitHubInstallationAsync(
        Guid ownerUserId,
        Guid projectId,
        long installationId,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(item => item.Id == projectId && item.OwnerUserId == ownerUserId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        project.SetGitHubInstallation(installationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<bool> DeleteAsync(Guid ownerUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(item => item.Id == projectId && item.OwnerUserId == ownerUserId, cancellationToken);

        if (project is null)
        {
            return false;
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ImportIssuesResult> ImportIssuesAsync(
        Guid ownerUserId,
        Guid projectId,
        int maxIssues = 100,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
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
        var issues = await gitHubIssueService.ListIssuesAsync(project.RepoOwner, project.RepoName, maxIssues, token, cancellationToken);

        var issueNumbers = issues.Select(issue => issue.IssueNumber).ToList();
        var existing = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId && task.GitHubIssueNumber.HasValue && issueNumbers.Contains(task.GitHubIssueNumber.Value))
            .Select(task => task.GitHubIssueNumber!.Value)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet();
        var imported = 0;
        var skipped = 0;

        foreach (var issue in issues)
        {
            if (existingSet.Contains(issue.IssueNumber))
            {
                skipped++;
                continue;
            }

            var status = issue.State.Equals("closed", StringComparison.OrdinalIgnoreCase)
                ? DomainTaskStatus.Done
                : DomainTaskStatus.Todo;

            var task = new DevBoard.Domain.Entities.Task(projectId, issue.Title, issue.CreatedAt.UtcDateTime);
            task.SetGitHubIssueNumber(issue.IssueNumber);

            if (status == DomainTaskStatus.Done)
            {
                task.ApplyGitHubStatus(DomainTaskStatus.Done, issue.UpdatedAt.UtcDateTime);
            }

            dbContext.Tasks.Add(task);
            imported++;
        }

        if (imported > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ImportIssuesResult(issues.Count, imported, skipped);
    }

    private static ProjectDto Map(Project project) =>
        new(project.Id, project.Name, project.RepoOwner, project.RepoName, project.CreatedAt, project.GitHubInstallationId.HasValue);
}
