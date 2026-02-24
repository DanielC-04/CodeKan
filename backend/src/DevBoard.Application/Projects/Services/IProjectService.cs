using DevBoard.Application.Projects.Dtos;

namespace DevBoard.Application.Projects.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(Guid ownerUserId, CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(Guid ownerUserId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid ownerUserId, Guid projectId, CancellationToken cancellationToken = default);
}
