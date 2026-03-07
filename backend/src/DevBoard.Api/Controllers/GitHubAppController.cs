using DevBoard.Application.Common;
using DevBoard.Application.Common.Interfaces;
using DevBoard.Domain.Entities;
using DevBoard.Infrastructure.GitHub;
using DevBoard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DevBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/github-app")]
public sealed class GitHubAppController(
    ApplicationDbContext dbContext,
    IGitHubAppTokenService gitHubAppTokenService,
    IConfiguration configuration,
    IOptions<GitHubAppOptions> options) : ControllerBase
{
    private readonly GitHubAppOptions _options = options.Value;

    [HttpPost("install-url")]
    [ProducesResponseType(typeof(ApiResponse<GitHubAppInstallUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GitHubAppInstallUrlResponse>>> GetInstallUrl(
        [FromBody] GitHubAppInstallUrlRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ProjectId && item.OwnerUserId == ownerUserId, cancellationToken);

        if (project is null)
        {
            return BadRequest(ApiResponse<object>.Fail("Project not found."));
        }

        if (string.IsNullOrWhiteSpace(_options.InstallUrl))
        {
            return BadRequest(ApiResponse<object>.Fail("GitHub App install URL is not configured."));
        }

        if (string.IsNullOrWhiteSpace(_options.StateSecret))
        {
            return BadRequest(ApiResponse<object>.Fail("GitHub App state secret is not configured."));
        }

        var nonce = GenerateNonce();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(10);
        var payload = BuildStatePayload(request.ProjectId, ownerUserId, now, nonce);
        var signature = SignState(payload, _options.StateSecret);
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)) + "." + signature;

        dbContext.GitHubInstallationNonces.Add(new GitHubInstallationNonce(request.ProjectId, ownerUserId, nonce, expiresAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        var installUrl = _options.InstallUrl.TrimEnd('/') + "?state=" + Uri.EscapeDataString(state);
        return Ok(ApiResponse<GitHubAppInstallUrlResponse>.Ok(new GitHubAppInstallUrlResponse(installUrl), "Install URL created."));
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] long installation_id, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var successUrl = $"{frontendBaseUrl.TrimEnd('/')}/kanban?github=ok";
        var errorUrl = $"{frontendBaseUrl.TrimEnd('/')}/kanban?github=error";

        if (installation_id <= 0 || string.IsNullOrWhiteSpace(state))
        {
            return Redirect(errorUrl);
        }

        if (!TryParseState(state, out var projectId, out var userId, out var issuedAt, out var nonce, out var signature))
        {
            return Redirect(errorUrl);
        }

        if (string.IsNullOrWhiteSpace(_options.StateSecret))
        {
            return Redirect(errorUrl);
        }

        var payload = BuildStatePayload(projectId, userId, issuedAt, nonce);
        var expectedSignature = SignState(payload, _options.StateSecret);
        if (!FixedTimeEquals(expectedSignature, signature))
        {
            return Redirect(errorUrl);
        }

        if (issuedAt.AddMinutes(10) < DateTime.UtcNow)
        {
            return Redirect(errorUrl);
        }

        var nonceEntity = await dbContext.GitHubInstallationNonces
            .FirstOrDefaultAsync(item => item.Nonce == nonce && item.ProjectId == projectId && item.UserId == userId, cancellationToken);

        if (nonceEntity is null || nonceEntity.ConsumedAt.HasValue || nonceEntity.ExpiresAt < DateTime.UtcNow)
        {
            return Redirect(errorUrl);
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(item => item.Id == projectId && item.OwnerUserId == userId, cancellationToken);

        if (project is null)
        {
            return Redirect(errorUrl);
        }

        var token = await gitHubAppTokenService.GetInstallationTokenAsync(installation_id, cancellationToken);
        var repoMatches = await IsRepoIncludedAsync(project.RepoOwner, project.RepoName, token, cancellationToken);
        if (!repoMatches)
        {
            return Redirect(errorUrl);
        }

        nonceEntity.Consume(DateTime.UtcNow);
        project.SetGitHubInstallation(installation_id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Redirect(successUrl);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }

    private static string BuildStatePayload(Guid projectId, Guid userId, DateTime issuedAt, string nonce)
    {
        return string.Join('|', projectId, userId, issuedAt.ToString("O"), nonce);
    }

    private static bool TryParseState(
        string state,
        out Guid projectId,
        out Guid userId,
        out DateTime issuedAt,
        out string nonce,
        out string signature)
    {
        projectId = Guid.Empty;
        userId = Guid.Empty;
        issuedAt = default;
        nonce = string.Empty;
        signature = string.Empty;

        var parts = state.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        signature = parts[1];
        byte[] payloadBytes;
        try
        {
            payloadBytes = Convert.FromBase64String(parts[0]);
        }
        catch (FormatException)
        {
            return false;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var payloadParts = payload.Split('|');
        if (payloadParts.Length != 4)
        {
            return false;
        }

        if (!Guid.TryParse(payloadParts[0], out projectId))
        {
            return false;
        }

        if (!Guid.TryParse(payloadParts[1], out userId))
        {
            return false;
        }

        if (!DateTime.TryParse(payloadParts[2], null, System.Globalization.DateTimeStyles.RoundtripKind, out issuedAt))
        {
            return false;
        }

        nonce = payloadParts[3];
        return true;
    }

    private static string SignState(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
    }

    private static string GenerateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task<bool> IsRepoIncludedAsync(
        string repoOwner,
        string repoName,
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("DevBoard"))
        {
            Credentials = new Octokit.Credentials(token)
        };

        var repositories = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
        return repositories.Repositories.Any(repo =>
            repo.Owner.Login.Equals(repoOwner, StringComparison.OrdinalIgnoreCase)
            && repo.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record GitHubAppInstallUrlRequest(Guid ProjectId);

public sealed record GitHubAppInstallUrlResponse(string InstallUrl);
