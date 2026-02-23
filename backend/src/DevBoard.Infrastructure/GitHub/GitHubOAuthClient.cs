using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DevBoard.Application.Auth.Models;
using DevBoard.Application.Auth.Services;
using Microsoft.Extensions.Options;

namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubOAuthClient(
    HttpClient httpClient,
    IOptions<GitHubOAuthOptions> options) : IGitHubOAuthClient
{
    private readonly GitHubOAuthOptions _options = options.Value;

    public string BuildAuthorizationUrl(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("OAuth state is required.");
        }

        EnsureConfigured();

        var parameters = new List<string>
        {
            $"client_id={Uri.EscapeDataString(_options.ClientId)}",
            $"scope={Uri.EscapeDataString(_options.Scope)}",
            $"state={Uri.EscapeDataString(state)}"
        };

        if (!string.IsNullOrWhiteSpace(_options.BackendCallbackUrl))
        {
            parameters.Add($"redirect_uri={Uri.EscapeDataString(_options.BackendCallbackUrl)}");
        }

        return $"{_options.AuthorizeUrl}?{string.Join("&", parameters)}";
    }

    public async Task<GitHubOAuthIdentity> ExchangeCodeForIdentityAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("GitHub authorization code is required.");
        }

        EnsureConfigured();

        using var tokenRequest = BuildTokenRequest(code);
        using var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to exchange GitHub authorization code.");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid GitHub token response.");

        if (string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
        {
            throw new InvalidOperationException("GitHub access token was not returned.");
        }

        var user = await GetUserAsync(tokenPayload.AccessToken, cancellationToken);
        var email = await ResolveVerifiedEmailAsync(tokenPayload.AccessToken, user.Email, cancellationToken);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("GitHub account does not expose a verified email.");
        }

        return new GitHubOAuthIdentity(
            user.Id.ToString(),
            user.Login,
            email.Trim().ToLowerInvariant());
    }

    private async Task<GitHubUserResponse> GetUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CodeKan", "1.0"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to read GitHub user profile.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Invalid GitHub user profile response.");

        if (payload.Id <= 0 || string.IsNullOrWhiteSpace(payload.Login))
        {
            throw new InvalidOperationException("GitHub user profile is incomplete.");
        }

        return payload;
    }

    private async Task<string?> ResolveVerifiedEmailAsync(string accessToken, string? profileEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(profileEmail))
        {
            return profileEmail;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserEmailsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CodeKan", "1.0"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var emails = await response.Content.ReadFromJsonAsync<IReadOnlyList<GitHubEmailResponse>>(cancellationToken: cancellationToken)
            ?? [];

        var primaryVerified = emails.FirstOrDefault(item => item.Verified && item.Primary && !string.IsNullOrWhiteSpace(item.Email));
        if (primaryVerified is not null)
        {
            return primaryVerified.Email;
        }

        var anyVerified = emails.FirstOrDefault(item => item.Verified && !string.IsNullOrWhiteSpace(item.Email));
        return anyVerified?.Email;
    }

    private HttpRequestMessage BuildTokenRequest(string code)
    {
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code.Trim()
        };

        if (!string.IsNullOrWhiteSpace(_options.BackendCallbackUrl))
        {
            payload["redirect_uri"] = _options.BackendCallbackUrl;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
        {
            Content = new FormUrlEncodedContent(payload)
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CodeKan", "1.0"));

        return request;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("GitHub OAuth client credentials are missing.");
        }
    }

    private sealed record GitHubTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("scope")] string Scope);

    private sealed record GitHubUserResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("email")] string? Email);

    private sealed record GitHubEmailResponse(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified);
}
