using DevBoard.Domain.Entities;

namespace DevBoard.Application.Auth.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
}
