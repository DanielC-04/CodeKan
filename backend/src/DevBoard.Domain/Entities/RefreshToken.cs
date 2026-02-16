using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? CreatedByIp { get; private set; }

    public AppUser User { get; private set; } = null!;

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt, string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("userId is required.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = ValidateRequired(tokenHash, nameof(tokenHash), 200);
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedByIp = string.IsNullOrWhiteSpace(createdByIp) ? null : createdByIp.Trim();
    }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void Revoke(DateTime? revokedAt = null)
    {
        RevokedAt = revokedAt ?? DateTime.UtcNow;
    }

    private static string ValidateRequired(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }
}
