using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DevBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubAppTokenService(IOptions<GitHubAppOptions> options) : IGitHubAppTokenService
{
    private readonly GitHubAppOptions _options = options.Value;

    public async Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            throw new InvalidOperationException("GitHub AppId is missing.");
        }

        if (string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            throw new InvalidOperationException("GitHub App private key is missing.");
        }

        var jwt = CreateJwt(_options.AppId, _options.PrivateKey);
        var client = new GitHubClient(new ProductHeaderValue("DevBoard"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        var response = await client.GitHubApps.CreateInstallationToken(installationId);
        if (string.IsNullOrWhiteSpace(response.Token))
        {
            throw new InvalidOperationException("GitHub installation token was not returned.");
        }

        return response.Token;
    }

    private static string CreateJwt(string appId, string privateKey)
    {
        if (!long.TryParse(appId, out var issuer))
        {
            throw new InvalidOperationException("GitHub AppId must be numeric.");
        }

        var now = DateTimeOffset.UtcNow;
        var key = CreateSecurityKey(privateKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var issuedAt = now.ToUnixTimeSeconds();
        var claims = new[]
        {
            new Claim("iat", issuedAt.ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer.ToString(),
            audience: null,
            claims: claims,
            notBefore: now.UtcDateTime.AddMinutes(-1),
            expires: now.UtcDateTime.AddMinutes(9),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SecurityKey CreateSecurityKey(string privateKey)
    {
        var normalized = privateKey
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Trim();

        var rsa = RSA.Create();
        rsa.ImportFromPem(normalized);
        return new RsaSecurityKey(rsa);
    }
}
