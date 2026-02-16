using System.Security.Cryptography;
using System.Text;
using DevBoard.Application.Auth.Dtos;
using DevBoard.Application.Auth.Models;
using DevBoard.Application.Auth.Services;
using DevBoard.Infrastructure.Persistence;
using DevBoard.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AppUserEntity = DevBoard.Domain.Entities.AppUser;
using RefreshTokenEntity = DevBoard.Domain.Entities.RefreshToken;

namespace DevBoard.Infrastructure.Services;

public sealed class AuthService(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const string DefaultUserRole = "Member";
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthSession> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var user = new AppUserEntity(normalizedEmail, passwordHash, DefaultUserRole);

        dbContext.Users.Add(user);
        var session = CreateSession(user, ipAddress);
        dbContext.RefreshTokens.Add(new RefreshTokenEntity(user.Id, HashToken(session.RefreshToken), session.RefreshTokenExpiresAt, ipAddress));

        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<AuthSession> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var session = CreateSession(user, ipAddress);
        dbContext.RefreshTokens.Add(new RefreshTokenEntity(user.Id, HashToken(session.RefreshToken), session.RefreshTokenExpiresAt, ipAddress));
        await dbContext.SaveChangesAsync(cancellationToken);

        return session;
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Invalid refresh token.");
        }

        if (!storedToken.User.IsActive)
        {
            throw new InvalidOperationException("User is inactive.");
        }

        storedToken.Revoke();
        var session = CreateSession(storedToken.User, ipAddress);

        dbContext.RefreshTokens.Add(new RefreshTokenEntity(
            storedToken.UserId,
            HashToken(session.RefreshToken),
            session.RefreshTokenExpiresAt,
            ipAddress));

        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async System.Threading.Tasks.Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private AuthSession CreateSession(AppUserEntity user, string? ipAddress)
    {
        _ = ipAddress;
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        return new AuthSession(
            user.Id,
            user.Email,
            user.Role,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken,
            refreshExpiresAt);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Refresh token is required.");
        }

        var bytes = Encoding.UTF8.GetBytes(token.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
