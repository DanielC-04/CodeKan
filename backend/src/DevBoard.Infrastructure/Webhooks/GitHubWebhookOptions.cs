namespace DevBoard.Infrastructure.Webhooks;

public sealed class GitHubWebhookOptions
{
    public string WebhookSecret { get; set; } = string.Empty;
}
