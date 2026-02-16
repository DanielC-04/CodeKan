namespace DevBoard.Application.Auth.Dtos;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    AuthUserDto User);
