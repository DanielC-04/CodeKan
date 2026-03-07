namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubAppOptions
{
    public string AppId { get; init; } = string.Empty;
    public string PrivateKey { get; init; } = string.Empty;
    public string InstallUrl { get; init; } = string.Empty;
    public string StateSecret { get; init; } = string.Empty;
}
