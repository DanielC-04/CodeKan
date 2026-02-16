using DevBoard.Domain.Exceptions;

namespace DevBoard.Domain.Entities;

public sealed class WebhookDelivery
{
    public Guid Id { get; private set; }
    public string DeliveryId { get; private set; }
    public string EventName { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    private WebhookDelivery()
    {
        DeliveryId = string.Empty;
        EventName = string.Empty;
    }

    public WebhookDelivery(string deliveryId, string eventName, DateTime? receivedAt = null)
    {
        Id = Guid.NewGuid();
        DeliveryId = ValidateRequired(deliveryId, nameof(deliveryId), 100);
        EventName = ValidateRequired(eventName, nameof(eventName), 50);
        ReceivedAt = receivedAt ?? DateTime.UtcNow;
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
