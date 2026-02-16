namespace DevBoard.Application.Auth.Models;

public sealed record AuthSession(
    Guid UserId,
    string Email,
    string Role,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
