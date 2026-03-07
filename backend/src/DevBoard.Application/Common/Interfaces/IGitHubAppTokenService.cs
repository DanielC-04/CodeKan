namespace DevBoard.Application.Common.Interfaces;

public interface IGitHubAppTokenService
{
    Task<string> GetAppJwtAsync(CancellationToken cancellationToken = default);
    Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken = default);
}
