using System.Security.Cryptography;
using System.Text;
using DevBoard.Application.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace DevBoard.Infrastructure.Webhooks;

public sealed class GitHubSignatureValidator(IOptions<GitHubWebhookOptions> options)
{
    private readonly GitHubWebhookOptions _options = options.Value;

    public void Validate(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            throw new InvalidOperationException("GitHub webhook secret is not configured.");
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            throw new InvalidWebhookSignatureException("Missing webhook signature.");
        }

        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidWebhookSignatureException("Invalid webhook signature format.");
        }

        var expectedHash = ComputeHash(payload, _options.WebhookSecret);
        var providedHash = signature["sha256=".Length..];

        if (!ConstantTimeEquals(expectedHash, providedHash))
        {
            throw new InvalidWebhookSignatureException("Webhook signature validation failed.");
        }
    }

    private static string ComputeHash(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
