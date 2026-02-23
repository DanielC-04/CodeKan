namespace DevBoard.Infrastructure.GitHub;

public sealed class GitHubOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BackendCallbackUrl { get; set; } = string.Empty;
    public string FrontendSuccessUrl { get; set; } = string.Empty;
    public string FrontendErrorUrl { get; set; } = string.Empty;
    public string AuthorizeUrl { get; set; } = "https://github.com/login/oauth/authorize";
    public string TokenUrl { get; set; } = "https://github.com/login/oauth/access_token";
    public string UserUrl { get; set; } = "https://api.github.com/user";
    public string UserEmailsUrl { get; set; } = "https://api.github.com/user/emails";
    public string Scope { get; set; } = "read:user user:email";
}
