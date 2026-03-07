using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; }
    public string RepoOwner { get; private set; }
    public string RepoName { get; private set; }
    public long? GitHubInstallationId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public AppUser Owner { get; private set; }

    private readonly List<Task> _tasks = [];
    public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();

    private Project()
    {
        Owner = null!;
        Name = string.Empty;
        RepoOwner = string.Empty;
        RepoName = string.Empty;
        GitHubInstallationId = null;
    }

    public Project(
        Guid ownerUserId,
        string name,
        string repoOwner,
        string repoName,
        long? gitHubInstallationId,
        DateTime? createdAt = null)
    {
        Id = Guid.NewGuid();
        OwnerUserId = ValidateRequired(ownerUserId, nameof(ownerUserId));
        Owner = null!;
        Name = ValidateRequired(name, nameof(name), 150);
        RepoOwner = ValidateRequired(repoOwner, nameof(repoOwner), 100);
        RepoName = ValidateRequired(repoName, nameof(repoName), 100);
        GitHubInstallationId = gitHubInstallationId;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = ValidateRequired(name, nameof(name), 150);
    }

    public void UpdateRepository(string repoOwner, string repoName)
    {
        RepoOwner = ValidateRequired(repoOwner, nameof(repoOwner), 100);
        RepoName = ValidateRequired(repoName, nameof(repoName), 100);
    }

    public void SetGitHubInstallation(long installationId)
    {
        if (installationId <= 0)
        {
            throw new DomainException("installationId must be positive.");
        }

        GitHubInstallationId = installationId;
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
