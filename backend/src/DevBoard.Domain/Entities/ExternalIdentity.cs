using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class ExternalIdentity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; }
    public string ProviderUserId { get; private set; }
    public string Email { get; private set; }
    public DateTime LinkedAt { get; private set; }

    public AppUser User { get; private set; }

    private ExternalIdentity()
    {
        Provider = string.Empty;
        ProviderUserId = string.Empty;
        Email = string.Empty;
        User = null!;
    }

    public ExternalIdentity(
        Guid userId,
        string provider,
        string providerUserId,
        string email,
        DateTime? linkedAt = null)
    {
        Id = Guid.NewGuid();
        UserId = ValidateRequired(userId, nameof(userId));
        Provider = ValidateRequired(provider, nameof(provider), 50).ToLowerInvariant();
        ProviderUserId = ValidateRequired(providerUserId, nameof(providerUserId), 200);
        Email = ValidateRequired(email, nameof(email), 200).ToLowerInvariant();
        LinkedAt = linkedAt ?? DateTime.UtcNow;
        User = null!;
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

    private static Guid ValidateRequired(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{field} is required.");
        }

        return value;
    }
}
