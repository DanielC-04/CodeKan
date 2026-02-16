namespace DevBoard.Application.Auth.Dtos;

public sealed record AuthUserDto(Guid Id, string Email, string Role);
