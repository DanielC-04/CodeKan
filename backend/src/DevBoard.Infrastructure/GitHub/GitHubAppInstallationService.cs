using DevBoard.Application.Common.Interfaces;
using Octokit;

namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubAppInstallationService(IGitHubAppTokenService tokenService) : IGitHubAppInstallationService
{
    public async Task<long?> GetInstallationIdForRepoAsync(string repoOwner, string repoName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jwt = await tokenService.GetAppJwtAsync(cancellationToken);
        var client = new GitHubClient(new ProductHeaderValue("DevBoard"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        try
        {
            var response = await client.Connection.Get<Installation>(
                new Uri($"repos/{repoOwner}/{repoName}/installation", UriKind.Relative),
                null,
                "application/vnd.github+json");

            return response.Body?.Id;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}
