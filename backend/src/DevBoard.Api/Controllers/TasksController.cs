using DevBoard.Application.Common;
using DevBoard.Application.Tasks.Dtos;
using DevBoard.Application.Tasks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBoard.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpPost("api/projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(projectId, request, cancellationToken);
        var response = ApiResponse<TaskDto>.Ok(task, "Task created successfully.");

        return CreatedAtAction(nameof(GetById), new { taskId = task.Id }, response);
    }

    [HttpGet("api/projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskDto>>>> GetByProjectId(Guid projectId, CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetByProjectIdAsync(projectId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TaskDto>>.Ok(tasks, "Tasks fetched successfully."));
    }

    [HttpGet("api/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            return NotFound(ApiResponse<TaskDto>.Fail("Task not found."));
        }

        return Ok(ApiResponse<TaskDto>.Ok(task, "Task fetched successfully."));
    }

    [HttpPatch("api/tasks/{taskId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateStatus(
        Guid taskId,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.UpdateStatusAsync(taskId, request, cancellationToken);
        if (task is null)
        {
            return NotFound(ApiResponse<TaskDto>.Fail("Task not found."));
        }

        return Ok(ApiResponse<TaskDto>.Ok(task, "Task status updated successfully."));
    }

    [HttpGet("api/tasks/{taskId:guid}/issue-details")]
    [ProducesResponseType(typeof(ApiResponse<GitHubIssueDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GitHubIssueDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GitHubIssueDetailsDto>>> GetIssueDetails(Guid taskId, CancellationToken cancellationToken)
    {
        var details = await taskService.GetIssueDetailsAsync(taskId, cancellationToken);
        if (details is null)
        {
            return NotFound(ApiResponse<GitHubIssueDetailsDto>.Fail("Task not found."));
        }

        return Ok(ApiResponse<GitHubIssueDetailsDto>.Ok(details, "Issue details fetched successfully."));
    }

    [HttpGet("api/tasks/{taskId:guid}/issue-comments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GitHubIssueCommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GitHubIssueCommentDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GitHubIssueCommentDto>>>> GetIssueComments(Guid taskId, CancellationToken cancellationToken)
    {
        var comments = await taskService.GetIssueCommentsAsync(taskId, cancellationToken);
        if (comments is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<GitHubIssueCommentDto>>.Fail("Task not found."));
        }

        return Ok(ApiResponse<IReadOnlyList<GitHubIssueCommentDto>>.Ok(comments, "Issue comments fetched successfully."));
    }
}
