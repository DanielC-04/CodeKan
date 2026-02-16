using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string RepoOwner { get; private set; }
    public string RepoName { get; private set; }
    public string GitHubTokenEncrypted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Task> _tasks = [];
    public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();

    private Project()
    {
        Name = string.Empty;
        RepoOwner = string.Empty;
        RepoName = string.Empty;
        GitHubTokenEncrypted = string.Empty;
    }

    public Project(
        string name,
        string repoOwner,
        string repoName,
        string gitHubTokenEncrypted,
        DateTime? createdAt = null)
    {
        Id = Guid.NewGuid();
        Name = ValidateRequired(name, nameof(name), 150);
        RepoOwner = ValidateRequired(repoOwner, nameof(repoOwner), 100);
        RepoName = ValidateRequired(repoName, nameof(repoName), 100);
        GitHubTokenEncrypted = ValidateRequired(gitHubTokenEncrypted, nameof(gitHubTokenEncrypted), 4000);
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

    public void RotateGitHubToken(string gitHubTokenEncrypted)
    {
        GitHubTokenEncrypted = ValidateRequired(gitHubTokenEncrypted, nameof(gitHubTokenEncrypted), 4000);
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
