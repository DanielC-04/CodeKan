using DevBoard.Application.Auth.Dtos;
using DevBoard.Application.Auth.Models;
using DevBoard.Application.Auth.Services;
using DevBoard.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "devboard_refresh";

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var session = await authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AuthTokenResponse>.Ok(Map(session), "User registered successfully."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);

        return Ok(ApiResponse<AuthTokenResponse>.Ok(Map(session), "Login successful."));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName]
            ?? throw new InvalidOperationException("Refresh token cookie is missing.");

        var session = await authService.RefreshAsync(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);

        return Ok(ApiResponse<AuthTokenResponse>.Ok(Map(session), "Token refreshed successfully."));
    }

    [HttpPost("revoke")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.RevokeAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });

        return Ok(ApiResponse<object>.Ok(new { }, "Session revoked successfully."));
    }

    private void SetRefreshCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/api/auth"
        });
    }

    private static AuthTokenResponse Map(AuthSession session) =>
        new(
            session.AccessToken,
            session.AccessTokenExpiresAt,
            new AuthUserDto(session.UserId, session.Email, session.Role));
}
