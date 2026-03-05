namespace DevBoard.Application.Projects.Dtos;

public sealed record ImportIssuesResult(
    int Total,
    int Imported,
    int Skipped);
