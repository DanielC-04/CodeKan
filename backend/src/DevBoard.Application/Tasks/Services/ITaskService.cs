using DevBoard.Application.Tasks.Dtos;

namespace DevBoard.Application.Tasks.Services;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(Guid ownerUserId, Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> GetByProjectIdAsync(Guid ownerUserId, Guid projectId, CancellationToken cancellationToken = default);
    Task<TaskDto?> GetByIdAsync(Guid ownerUserId, Guid taskId, CancellationToken cancellationToken = default);
    Task<TaskDto?> UpdateStatusAsync(Guid ownerUserId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
    Task<GitHubIssueDetailsDto?> GetIssueDetailsAsync(Guid ownerUserId, Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GitHubIssueCommentDto>?> GetIssueCommentsAsync(Guid ownerUserId, Guid taskId, CancellationToken cancellationToken = default);
}
