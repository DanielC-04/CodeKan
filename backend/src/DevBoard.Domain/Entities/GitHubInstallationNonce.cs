namespace DevBoard.Domain.Entities;

public sealed class GitHubInstallationNonce
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Nonce { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private GitHubInstallationNonce()
    {
        Nonce = string.Empty;
    }

    public GitHubInstallationNonce(Guid projectId, Guid userId, string nonce, DateTime expiresAt)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("projectId is required.", nameof(projectId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("userId is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(nonce))
        {
            throw new ArgumentException("nonce is required.", nameof(nonce));
        }

        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
        Nonce = nonce.Trim();
        ExpiresAt = expiresAt;
    }

    public void Consume(DateTime consumedAt)
    {
        ConsumedAt = consumedAt;
    }
}
