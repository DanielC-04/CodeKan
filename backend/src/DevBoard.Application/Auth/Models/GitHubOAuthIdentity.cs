namespace DevBoard.Application.Auth.Models;

public sealed record GitHubOAuthIdentity(
    string ProviderUserId,
    string Login,
    string Email);
