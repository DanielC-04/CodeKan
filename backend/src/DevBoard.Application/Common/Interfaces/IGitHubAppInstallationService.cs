namespace DevBoard.Application.Common.Interfaces;

public interface IGitHubAppInstallationService
{
    Task<long?> GetInstallationIdForRepoAsync(string repoOwner, string repoName, CancellationToken cancellationToken = default);
}
