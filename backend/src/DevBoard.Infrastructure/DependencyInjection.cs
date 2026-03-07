using DevBoard.Infrastructure.Persistence;
using DevBoard.Infrastructure.Services;
using DevBoard.Infrastructure.GitHub;
using DevBoard.Infrastructure.Webhooks;
using DevBoard.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Auth.Services;
using DevBoard.Application.Projects.Services;
using DevBoard.Application.Tasks.Services;
using DevBoard.Application.Webhooks.Services;

namespace DevBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DevBoardDb")
            ?? throw new InvalidOperationException("Connection string 'DevBoardDb' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<GitHubWebhookOptions>(configuration.GetSection("GitHub"));
        services.Configure<GitHubOAuthOptions>(configuration.GetSection("GitHubOAuth"));
        services.Configure<GitHubAppOptions>(configuration.GetSection("GitHubApp"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IGitHubIssueService, GitHubIssueService>();
        services.AddSingleton<IGitHubAppTokenService, GitHubAppTokenService>();
        services.AddHttpClient<IGitHubOAuthClient, GitHubOAuthClient>();
        services.AddScoped<GitHubSignatureValidator>();
        services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();
        
        return services;
    }
}
