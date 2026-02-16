namespace DevBoard.Application.Common.Exceptions;

public sealed class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException(string message)
        : base(message)
    {
    }
}
