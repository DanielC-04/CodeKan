using DevBoard.Application.Projects.Dtos;

namespace DevBoard.Application.Projects.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
