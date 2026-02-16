namespace DevBoard.Application.Tasks.Dtos;

public sealed record CreateTaskRequest(string Title, string? Description = null);
