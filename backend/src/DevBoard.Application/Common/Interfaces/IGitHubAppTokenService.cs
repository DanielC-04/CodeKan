namespace DevBoard.Application.Common.Interfaces;

public interface IGitHubAppTokenService
{
    Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken = default);
}
