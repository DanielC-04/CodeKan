using DevBoard.Application.Auth.Dtos;
using DevBoard.Application.Auth.Models;

namespace DevBoard.Application.Auth.Services;

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthSession> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthSession> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
