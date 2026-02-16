namespace DevBoard.Application.Webhooks.Services;

public interface IGitHubWebhookService
{
    Task ProcessAsync(
        string eventName,
        string deliveryId,
        string signature,
        string payload,
        CancellationToken cancellationToken = default);
}
