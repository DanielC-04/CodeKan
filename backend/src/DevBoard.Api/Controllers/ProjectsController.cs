using DevBoard.Application.Common;
using DevBoard.Application.Projects.Dtos;
using DevBoard.Application.Projects.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        var response = ApiResponse<ProjectDto>.Ok(project, "Project created successfully.");

        return CreatedAtAction(nameof(GetById), new { projectId = project.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProjectDto>>.Ok(projects, "Projects fetched successfully."));
    }

    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return NotFound(ApiResponse<ProjectDto>.Fail("Project not found."));
        }

        return Ok(ApiResponse<ProjectDto>.Ok(project, "Project fetched successfully."));
    }
}
