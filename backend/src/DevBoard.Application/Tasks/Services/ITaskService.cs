using DevBoard.Application.Tasks.Dtos;

namespace DevBoard.Application.Tasks.Services;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskDto?> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
    Task<GitHubIssueDetailsDto?> GetIssueDetailsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GitHubIssueCommentDto>?> GetIssueCommentsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
