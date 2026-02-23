using DevBoard.Application.Auth.Models;

namespace DevBoard.Application.Auth.Services;

public interface IGitHubOAuthClient
{
    string BuildAuthorizationUrl(string state);
    Task<GitHubOAuthIdentity> ExchangeCodeForIdentityAsync(string code, CancellationToken cancellationToken = default);
}
