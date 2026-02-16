using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private AppUser()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = string.Empty;
    }

    public AppUser(string email, string passwordHash, string role, DateTime? createdAt = null)
    {
        Id = Guid.NewGuid();
        Email = ValidateRequired(email, nameof(email), 200).ToLowerInvariant();
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500);
        Role = ValidateRequired(role, nameof(role), 50);
        IsActive = true;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
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
