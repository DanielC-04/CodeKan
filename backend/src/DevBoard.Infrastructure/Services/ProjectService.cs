using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Projects.Dtos;
using DevBoard.Application.Projects.Services;
using DevBoard.Domain.Entities;
using DevBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevBoard.Infrastructure.Services;

public sealed class ProjectService(ApplicationDbContext dbContext, ITokenProtector tokenProtector) : IProjectService
{
    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var encryptedToken = tokenProtector.Protect(request.GitHubToken);

        var project = new Project(
            request.Name,
            request.RepoOwner,
            request.RepoName,
            encryptedToken);

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => Map(project))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);

        return project is null ? null : Map(project);
    }

    private static ProjectDto Map(Project project) =>
        new(project.Id, project.Name, project.RepoOwner, project.RepoName, project.CreatedAt);
}
