using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Projects.Dtos;
using DevBoard.Application.Projects.Services;
using DevBoard.Domain.Entities;
using DevBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevBoard.Infrastructure.Services;

public sealed class ProjectService(ApplicationDbContext dbContext, ITokenProtector tokenProtector) : IProjectService
{
    public async Task<ProjectDto> CreateAsync(Guid ownerUserId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var encryptedToken = tokenProtector.Protect(request.GitHubToken);

        var project = new Project(
            ownerUserId,
            request.Name,
            request.RepoOwner,
            request.RepoName,
            encryptedToken);

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

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

    private static ProjectDto Map(Project project) =>
        new(project.Id, project.Name, project.RepoOwner, project.RepoName, project.CreatedAt);
}
