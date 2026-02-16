namespace DevBoard.Application.Common.Exceptions;

public sealed class GitHubIntegrationException : Exception
{
    public GitHubIntegrationException(string message)
        : base(message)
    {
    }

    public GitHubIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
